using System.Reflection;
using TightWiki.Library.Api;
using TightWiki.Library.Dummy;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;

namespace TightWiki.Tests.Unit
{
    /// <summary>
    /// These use the pages, namespaces and tags of the seed database and look them up first,
    /// so they keep passing as the seed pages change.
    /// </summary>
    [Collection("Markup Tests")]
    public class ApiReadServiceTests(TwEngineFixture fixture)
        : IClassFixture<TwEngineFixture>
    {
        /// <summary>A session that is allowed everything, except for pages for which <paramref name="denyWhen"/> is true.</summary>
        private class SelectiveSession : DispatchProxy
        {
            public ITwSessionState Inner { get; set; } = null!;
            public Func<string?, bool> DenyWhen { get; set; } = _ => false;

            protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
            {
                if (targetMethod?.Name == nameof(ITwSessionState.HoldsPermission) && args?.Length == 2 && args[0] is string or null)
                {
                    return Task.FromResult(!DenyWhen(args[0] as string));
                }
                return targetMethod?.Invoke(Inner, args);
            }
        }

        private ApiReadService CreateService(Func<string?, bool> denyWhen)
        {
            var session = DispatchProxy.Create<ITwSessionState, SelectiveSession>();
            var proxy = (SelectiveSession)(object)session;
            proxy.Inner = fixture.CreateWikiSession();
            proxy.DenyWhen = denyWhen;

            return new ApiReadService(session, fixture.Artifacts.Engine, fixture.Artifacts.Localizer);
        }

        private ApiReadService CreateService() => CreateService(_ => false);

        private ITwPageRepository Repository => fixture.Artifacts.DatabaseManager.PageRepository;

        private async Task<string> GetFirstNamespace()
        {
            foreach (var name in await Repository.GetAllNamespaces())
            {
                if (string.IsNullOrEmpty(name) == false)
                {
                    return name;
                }
            }
            throw new Exception("The seed database has no namespaces.");
        }

        private async Task<string> GetExistingNavigation()
        {
            var pages = await Repository.GetPageInfoByNamespaces([await GetFirstNamespace()]);
            return pages.First().Navigation;
        }

        #region Page content.

        [Fact]
        public async Task MarkupFormatReturnsTheSavedMarkup()
        {
            var navigation = await GetExistingNavigation();
            var saved = await Repository.GetPageRevisionByNavigation(navigation);

            var page = await CreateService().GetPage(navigation, null, ApiPageFormat.Markup);

            Assert.NotNull(page);
            Assert.Equal(saved!.Body, page.Body);
            Assert.Equal(saved.Name, page.Name);
            Assert.Equal(saved.Navigation, page.Navigation);
        }

        [Fact]
        public async Task HtmlFormatReturnsTheTransformedPage()
        {
            var navigation = await GetExistingNavigation();
            var saved = await Repository.GetPageRevisionByNavigation(navigation);
            var expected = await fixture.Artifacts.Engine.Transform(fixture.Artifacts.Localizer, fixture.CreateWikiSession(), saved!);

            var page = await CreateService().GetPage(navigation, null, ApiPageFormat.Html);

            Assert.NotNull(page);
            Assert.Equal(expected.HtmlResult, page.Body);
            Assert.NotEqual(saved!.Body, page.Body);
            Assert.Contains("<", page.Body);
        }

        [Fact]
        public async Task TextFormatReturnsTheTransformedPageWithoutTags()
        {
            var navigation = await GetExistingNavigation();
            var html = await CreateService().GetPage(navigation, null, ApiPageFormat.Html);

            var page = await CreateService().GetPage(navigation, null, ApiPageFormat.Text);

            Assert.NotNull(page);
            Assert.NotEmpty(page.Body);
            Assert.DoesNotContain("<", page.Body);
            Assert.True(page.Body.Length < html!.Body.Length);
        }

        [Fact]
        public async Task ANonExistingPageIsNull()
        {
            var page = await CreateService().GetPage("this_page_does_not_exist", null, ApiPageFormat.Markup);

            Assert.Null(page);
        }

        [Fact]
        public async Task ASpecificRevisionIsReturned()
        {
            //The seed pages have only one revision, so a second one is saved to a page of our own.
            var author = await Repository.GetPageRevisionByNavigation(await GetExistingNavigation());
            var pageName = "ApiReadServiceTests revision page";
            var testPage = new TwPage()
            {
                Name = pageName,
                Navigation = TwNamespaceNavigation.CleanAndValidate(pageName),
                Body = "The first revision.",
                CreatedByUserId = author!.CreatedByUserId,
                ModifiedByUserId = author.ModifiedByUserId,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };
            var localizer = new TwVerbatimLocalizationText();
            await Repository.UpsertPage(fixture.Artifacts.Engine, localizer, testPage);
            testPage.Body = "The second revision.";
            await Repository.UpsertPage(fixture.Artifacts.Engine, localizer, testPage);

            var first = await CreateService().GetPage(testPage.Navigation, 1, ApiPageFormat.Markup);
            var latest = await CreateService().GetPage(testPage.Navigation, null, ApiPageFormat.Markup);

            Assert.NotNull(first);
            Assert.Equal(1, first.Revision);
            Assert.Equal("The first revision.", first.Body.Trim());
            Assert.True(first.MostCurrentRevision > first.Revision);
            Assert.NotNull(latest);
            Assert.Equal("The second revision.", latest.Body.Trim());
        }

        [Fact]
        public async Task ANonExistingRevisionIsNull()
        {
            var navigation = await GetExistingNavigation();

            var page = await CreateService().GetPage(navigation, 99999, ApiPageFormat.Markup);

            Assert.Null(page);
        }

        [Fact]
        public async Task APageThatCannotBeReadIsForbidden()
        {
            var navigation = await GetExistingNavigation();
            var service = CreateService(o => o == navigation);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetPage(navigation, null, ApiPageFormat.Markup));
        }

        #endregion

        #region Revisions.

        [Fact]
        public async Task RevisionHistoryListsTheRevisionsOfThePage()
        {
            var navigation = await GetExistingNavigation();

            var revisions = await CreateService().GetPageRevisions(navigation, 1);

            Assert.NotNull(revisions);
            Assert.Equal(1, revisions.PageNumber);
            Assert.NotEmpty(revisions.Items);
            Assert.Contains(revisions.Items, o => o.Revision == 1);
        }

        [Fact]
        public async Task RevisionHistoryOfANonExistingPageIsNull()
        {
            Assert.Null(await CreateService().GetPageRevisions("this_page_does_not_exist", 1));
        }

        [Fact]
        public async Task RevisionHistoryOfAPageThatCannotBeReadIsForbidden()
        {
            var navigation = await GetExistingNavigation();
            var service = CreateService(o => o == navigation);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetPageRevisions(navigation, 1));
        }

        #endregion

        #region Search.

        [Fact]
        public async Task SearchReturnsTheMatchingPages()
        {
            var all = await Repository.PageSearch(["test"]);
            Assert.NotEmpty(all);

            var result = await CreateService().SearchPages("test", 1);

            Assert.NotEmpty(result.Items);
            Assert.Equal(1, result.PageNumber);
            Assert.Equal(all.First().Navigation, result.Items.First().Navigation);
        }

        [Fact]
        public async Task SearchWithoutTextReturnsNothing()
        {
            var result = await CreateService().SearchPages("  ", 1);

            Assert.Empty(result.Items);
            Assert.Equal(0, result.PageCount);
        }

        [Fact]
        public async Task SearchLeavesOutThePagesThatCannotBeRead()
        {
            var all = await Repository.PageSearch(["test"]);
            var denied = all.First().Navigation;

            var unfiltered = await CreateService().SearchPages("test", 1);
            var result = await CreateService(o => o == denied).SearchPages("test", 1);

            Assert.Contains(unfiltered.Items, o => o.Navigation == denied);
            Assert.DoesNotContain(result.Items, o => o.Navigation == denied);
        }

        [Fact]
        public async Task SearchFindsNothingWhenNothingCanBeRead()
        {
            Assert.NotEmpty(await Repository.PageSearch(["test"]));

            var result = await CreateService(_ => true).SearchPages("test", 1);

            Assert.Empty(result.Items);
            Assert.Equal(0, result.PageCount);
        }

        [Fact]
        public async Task SearchClampsThePageNumber()
        {
            var result = await CreateService().SearchPages("test", 99999);

            Assert.Equal(result.PageCount, result.PageNumber);
            Assert.NotEmpty(result.Items);
        }

        #endregion

        #region Namespaces.

        [Fact]
        public async Task NamespacesListsEveryNamespaceThatHasAReadablePage()
        {
            var expected = (await Repository.GetAllNamespaces()).Where(o => string.IsNullOrEmpty(o) == false).Order().ToList();
            Assert.NotEmpty(expected);

            var namespaces = await CreateService().GetNamespaces();

            Assert.Equal(expected, namespaces);
        }

        [Fact]
        public async Task ANamespaceWithoutAReadablePageIsNotListed()
        {
            var hiddenNamespace = await GetFirstNamespace();
            var hiddenNavigations = (await Repository.GetPageInfoByNamespaces([hiddenNamespace])).Select(o => o.Navigation).ToList();

            var namespaces = await CreateService(o => hiddenNavigations.Contains(o ?? string.Empty)).GetNamespaces();

            Assert.DoesNotContain(hiddenNamespace, namespaces);
            Assert.NotEmpty(namespaces);
        }

        [Fact]
        public async Task ANamespaceWithOneReadablePageIsListed()
        {
            var visibleNamespace = await GetFirstNamespace();
            var navigations = (await Repository.GetPageInfoByNamespaces([visibleNamespace])).Select(o => o.Navigation).ToList();
            var onlyReadable = navigations.First();

            var namespaces = await CreateService(o => o != onlyReadable).GetNamespaces();

            Assert.Contains(visibleNamespace, namespaces);
        }

        [Fact]
        public async Task NamespacesIsEmptyWhenNothingCanBeRead()
        {
            Assert.Empty(await CreateService(_ => true).GetNamespaces());
        }

        [Fact]
        public async Task NamespacePagesListsTheReadablePages()
        {
            var namespaceName = await GetFirstNamespace();
            var all = await Repository.GetPageInfoByNamespaces([namespaceName]);
            var denied = all.First().Navigation;

            var everything = await CreateService().GetNamespacePages(namespaceName);
            var filtered = await CreateService(o => o == denied).GetNamespacePages(namespaceName);

            Assert.Equal(all.Count, everything.Count);
            Assert.DoesNotContain(filtered, o => o.Navigation == denied);
            Assert.Equal(all.Count - 1, filtered.Count);
            Assert.Empty(await CreateService(_ => true).GetNamespacePages(namespaceName));
        }

        #endregion

        #region Tags.

        [Fact]
        public async Task TagPagesListsTheReadablePages()
        {
            var firstPage = (await Repository.GetPageInfoByNamespaces([await GetFirstNamespace()])).First();
            var tag = (await Repository.GetPageTagsById(firstPage.Id)).First().Tag;
            var all = await Repository.GetPageInfoByTag(tag);
            Assert.Contains(all, o => o.Navigation == firstPage.Navigation);

            var everything = await CreateService().GetTagPages(tag);
            var filtered = await CreateService(o => o == firstPage.Navigation).GetTagPages(tag);

            Assert.Equal(all.Count, everything.Count);
            Assert.DoesNotContain(filtered, o => o.Navigation == firstPage.Navigation);
            Assert.Equal(all.Count - 1, filtered.Count);
            Assert.Empty(await CreateService(_ => true).GetTagPages(tag));
        }

        #endregion
    }
}
