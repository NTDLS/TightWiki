using NTDLS.Helpers;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki.Library.Api
{
    /// <summary>
    /// Reads wiki content on behalf of the session user. Every method checks the Read permission of the session
    /// before it returns anything, the same way the web pages do.
    /// Methods that are asked for one page return null when the page does not exist and throw
    /// <see cref="UnauthorizedAccessException"/> when the session may not read it.
    /// Methods that return lists leave out the pages that the session may not read.
    /// </summary>
    public class ApiReadService(ITwSessionState session, ITwEngine engine, ITwSharedLocalizationText localizer)
    {
        /// <summary>The longest search text that is accepted, anything beyond this is ignored.</summary>
        private const int MaximumSearchStringLength = 256;

        private ITwPageRepository PageRepository => engine.DatabaseManager.PageRepository;

        public async Task<ApiPagedList<ApiPageSummary>> SearchPages(string? searchText, int pageNumber)
        {
            var searchString = (searchText ?? string.Empty).Trim();
            if (searchString.Length > MaximumSearchStringLength)
            {
                searchString = searchString.Substring(0, MaximumSearchStringLength);
            }

            var result = new ApiPagedList<ApiPageSummary>();
            if (searchString.Length == 0)
            {
                return result;
            }

            //Only pages that the user is allowed to read are listed, so this is done before paging.
            var readablePages = await TwSearchResults.GetReadablePages(session, PageRepository,
                engine.DatabaseManager.UsersRepository, Utility.SplitToTokens(searchString));

            var pageSize = Math.Max(1, await engine.DatabaseManager.ConfigurationRepository.Get<int>(TwConfigGroup.Customization, "Pagination Size"));

            result.PageCount = (readablePages.Count + pageSize - 1) / pageSize;
            result.PageNumber = Math.Clamp(pageNumber, 1, Math.Max(1, result.PageCount));
            result.Items = ToSummaries(readablePages.Skip((result.PageNumber - 1) * pageSize).Take(pageSize));

            return result;
        }

        public async Task<ApiPageContent?> GetPage(string givenCanonical, int? revision, ApiPageFormat format)
        {
            await RequireRead(givenCanonical);

            var page = await PageRepository.GetPageRevisionByNavigation(givenCanonical, revision);
            if (page == null)
            {
                return null;
            }

            var content = new ApiPageContent()
            {
                Name = page.Name,
                Navigation = page.Navigation,
                Description = page.Description,
                Revision = page.Revision,
                MostCurrentRevision = page.MostCurrentRevision,
                ModifiedByUserName = page.ModifiedByUserName,
                ModifiedDate = page.ModifiedDate
            };

            if (format == ApiPageFormat.Markup)
            {
                content.Body = page.Body;
            }
            else
            {
                var state = await engine.Transform(localizer, session, page, revision);
                content.Body = format == ApiPageFormat.Text ? Html.StripHtml(state.HtmlResult) : state.HtmlResult;
            }

            return content;
        }

        public async Task<ApiPagedList<ApiPageRevision>?> GetPageRevisions(string givenCanonical, int pageNumber)
        {
            await RequireRead(givenCanonical);

            var navigation = TwNamespaceNavigation.CleanAndValidate(givenCanonical);
            if (await PageRepository.GetPageInfoByNavigation(navigation) == null)
            {
                return null;
            }

            var revisions = await PageRepository.GetPageRevisionsInfoByNavigationPaged(navigation, Math.Max(1, pageNumber));

            var result = new ApiPagedList<ApiPageRevision>()
            {
                PageNumber = Math.Max(1, pageNumber),
                PageCount = revisions.FirstOrDefault()?.PaginationPageCount ?? 0
            };

            foreach (var revision in revisions)
            {
                result.Items.Add(new ApiPageRevision()
                {
                    Revision = revision.Revision,
                    ModifiedByUserName = revision.ModifiedByUserName,
                    ModifiedDate = revision.ModifiedDate,
                    ChangeSummary = revision.ChangeSummary
                });
            }

            return result;
        }

        /// <summary>
        /// Returns the namespaces in which the session can read at least one page.
        /// </summary>
        public async Task<List<string>> GetNamespaces()
        {
            var allNamespaces = new List<string>();
            foreach (var name in await PageRepository.GetAllNamespaces())
            {
                if (string.IsNullOrEmpty(name) == false)
                {
                    allNamespaces.Add(name);
                }
            }

            var readableNamespaces = new List<string>();
            var pages = await PageRepository.GetPageInfoByNamespaces(allNamespaces);
            foreach (var page in await GetReadablePages(pages))
            {
                if (readableNamespaces.Contains(page.Namespace) == false)
                {
                    readableNamespaces.Add(page.Namespace);
                }
            }

            readableNamespaces.Sort();
            return readableNamespaces;
        }

        public async Task<List<ApiPageSummary>> GetNamespacePages(string namespaceName)
        {
            var pages = await PageRepository.GetPageInfoByNamespaces([namespaceName]);
            var readablePages = await GetReadablePages(pages);
            return ToSummaries(readablePages.OrderBy(o => o.Name));
        }

        public async Task<List<ApiPageSummary>> GetTagPages(string tag)
        {
            var pages = await PageRepository.GetPageInfoByTag(tag);
            var readablePages = await GetReadablePages(pages);
            return ToSummaries(readablePages.OrderBy(o => o.Name));
        }

        private async Task RequireRead(string givenCanonical)
        {
            if (await session.HoldsPermission(givenCanonical, Plugin.TwPermission.Read) == false)
            {
                throw new UnauthorizedAccessException("You do not have permission to read this page.");
            }
        }

        private async Task<List<TwPage>> GetReadablePages(List<TwPage> pages)
        {
            var readablePages = new List<TwPage>();

            foreach (var page in pages)
            {
                if (await session.HoldsPermission(page.Navigation, Plugin.TwPermission.Read))
                {
                    readablePages.Add(page);
                }
            }

            return readablePages;
        }

        private static List<ApiPageSummary> ToSummaries(IEnumerable<TwPage> pages)
        {
            var summaries = new List<ApiPageSummary>();

            foreach (var page in pages)
            {
                summaries.Add(new ApiPageSummary()
                {
                    Name = page.Name,
                    Navigation = page.Navigation,
                    Description = page.Description
                });
            }

            return summaries;
        }
    }
}
