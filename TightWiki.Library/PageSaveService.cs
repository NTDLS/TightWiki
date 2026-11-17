using NTDLS.Helpers;
using TightWiki.Library.Caching;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki.Library
{
    /// <summary>
    /// Creates and edits pages on behalf of the session user. The checks are the ones that the wiki applies when a page is
    /// saved, in the same order. When a check fails nothing is saved and the result tells which check it was.
    /// </summary>
    public class PageSaveService(ITwSessionState session, ITwEngine engine, ITwSharedLocalizationText localizer)
    {
        private ITwPageRepository PageRepository => engine.DatabaseManager.PageRepository;

        /// <summary>
        /// Saves a new page when <paramref name="pageId"/> is zero, otherwise a new revision of the existing page.
        /// </summary>
        /// <param name="revision">The revision that the caller has based the edit on, only used for an existing page.</param>
        public async Task<PageSaveResult> SavePage(int pageId, int revision, string name, string? body, string? description, string? changeSummary)
        {
            if (engine.WikiConfiguration.ShowChangeSummaryWhenEditing
                && engine.WikiConfiguration.RequireChangeSummaryWhenEditing
                && string.IsNullOrEmpty(changeSummary))
            {
                return Failed(PageSaveError.ChangeSummaryRequired);
            }

            if (Utility.PageNameContainsUnsafeCharacters(name))
            {
                return Failed(PageSaveError.NameContainsUnsafeCharacters);
            }

            if (Utility.CountOccurrencesOf(name, "::") > 1)
            {
                return Failed(PageSaveError.NameContainsMultipleNamespaceSeparators);
            }

            if (pageId == 0)
            {
                return await CreatePage(name, body, description, changeSummary);
            }

            return await EditPage(pageId, revision, name, body, description, changeSummary);
        }

        private async Task<PageSaveResult> CreatePage(string name, string? body, string? description, string? changeSummary)
        {
            var navigation = TwNamespaceNavigation.CleanAndValidate(name);

            if (await session.HoldsPermission(navigation, Plugin.TwPermission.Create) == false)
            {
                return Failed(PageSaveError.CreateNotPermitted);
            }

            var page = new TwPage()
            {
                CreatedDate = DateTime.UtcNow,
                CreatedByUserId = session.Profile.EnsureNotNull().UserId,
                ModifiedDate = DateTime.UtcNow,
                ModifiedByUserId = session.Profile.UserId,
                Body = body ?? "",
                Name = name,
                ChangeSummary = changeSummary ?? string.Empty,
                Navigation = navigation,
                Description = description ?? ""
            };

            if (await PageRepository.GetPageInfoByNavigation(page.Navigation) != null)
            {
                return Failed(PageSaveError.NameAlreadyExists);
            }

            page.Id = await PageRepository.UpsertPage(engine, localizer, page, session);

            return new PageSaveResult() { Page = page };
        }

        private async Task<PageSaveResult> EditPage(int pageId, int revision, string name, string? body, string? description, string? changeSummary)
        {
            var navigation = TwNamespaceNavigation.CleanAndValidate(name);

            if (await session.HoldsPermission(navigation, Plugin.TwPermission.Edit) == false)
            {
                return Failed(PageSaveError.EditNotPermitted);
            }

            var page = (await PageRepository.GetPageRevisionById(pageId)).EnsureNotNull();
            var instructions = await PageRepository.GetPageProcessingInstructionsByPageId(page.Id);
            if (instructions.Contains(TwInstruction.Protect) && !await session.HoldsPermission(navigation, Plugin.TwPermission.Moderate))
            {
                return Failed(PageSaveError.PageProtected);
            }

            if (page.Revision != revision)
            {
                return Failed(PageSaveError.RevisionConflict);
            }

            string originalNavigation = string.Empty;

            if (!page.Navigation.Equals(navigation, StringComparison.InvariantCultureIgnoreCase))
            {
                if (await PageRepository.GetPageInfoByNavigation(navigation) != null)
                {
                    return Failed(PageSaveError.NameAlreadyExists);
                }

                originalNavigation = page.Navigation; //So we can clear cache and this also indicates that the page was renamed.
            }

            page.ModifiedDate = DateTime.UtcNow;
            page.ModifiedByUserId = session.Profile.EnsureNotNull().UserId;
            page.Body = body ?? "";
            page.Name = name;
            page.ChangeSummary = changeSummary ?? string.Empty;
            page.Navigation = navigation;
            page.Description = description ?? "";

            await PageRepository.UpsertPage(engine, localizer, page, session);

            if (string.IsNullOrWhiteSpace(originalNavigation) == false)
            {
                MemCache.ClearCategory(MemCacheKey.Build(MemCache.Category.Page, [originalNavigation]));
                MemCache.ClearCategory(MemCacheKey.Build(MemCache.Category.Page, [page.Id]));
            }

            return new PageSaveResult() { Page = page, OriginalNavigation = originalNavigation };
        }

        private static PageSaveResult Failed(PageSaveError error)
        {
            return new PageSaveResult() { Error = error };
        }
    }
}
