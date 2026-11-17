using System.Reflection;
using TightWiki.Library;
using TightWiki.Library.Dummy;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;
using TwPermission = TightWiki.Plugin.TwPermission;

namespace TightWiki.Tests.Unit
{
    [Collection("Markup Tests")]
    public class PageSaveServiceTests(TwEngineFixture fixture)
        : IClassFixture<TwEngineFixture>
    {
        /// <summary>A session that is allowed everything, except for the permissions for which <paramref name="denyWhen"/> is true.</summary>
        private class SelectiveSession : DispatchProxy
        {
            public ITwSessionState Inner { get; set; } = null!;
            public Func<TwPermission, bool> DenyWhen { get; set; } = _ => false;

            protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
            {
                if (targetMethod?.Name == nameof(ITwSessionState.HoldsPermission) && args?.Length == 2 && args[1] is TwPermission permission)
                {
                    return Task.FromResult(!DenyWhen(permission));
                }
                return targetMethod?.Invoke(Inner, args);
            }
        }

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

        private async Task<Guid> GetAuthorUserId()
        {
            var pages = await Repository.GetPageInfoByNamespaces([await GetFirstNamespace()]);
            var author = await Repository.GetPageRevisionByNavigation(pages.First().Navigation);
            return author!.ModifiedByUserId;
        }

        private async Task<PageSaveService> CreateService(Func<TwPermission, bool> denyWhen)
        {
            var inner = new TwDummySessionState()
            {
                Profile = new TwAccountProfile() { UserId = await GetAuthorUserId() }
            };
            var session = DispatchProxy.Create<ITwSessionState, SelectiveSession>();
            var proxy = (SelectiveSession)(object)session;
            proxy.Inner = inner;
            proxy.DenyWhen = denyWhen;

            return new PageSaveService(session, fixture.Artifacts.Engine, fixture.Artifacts.Localizer);
        }

        private Task<PageSaveService> CreateService() => CreateService(_ => false);

        /// <summary>Saves a page without the service, so a test can start from an existing page.</summary>
        private async Task<TwPage> CreateExistingPage(string name, string body)
        {
            var userId = await GetAuthorUserId();
            var page = new TwPage()
            {
                Name = name,
                Navigation = TwNamespaceNavigation.CleanAndValidate(name),
                Body = body,
                CreatedByUserId = userId,
                ModifiedByUserId = userId,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };
            page.Id = await Repository.UpsertPage(fixture.Artifacts.Engine, new TwVerbatimLocalizationText(), page);
            return (await Repository.GetPageRevisionById(page.Id))!;
        }

        #region Validation.

        [Fact]
        public async Task ANameWithUnsafeCharactersIsRejected()
        {
            var result = await (await CreateService()).SavePage(0, 0, "PageSaveServiceTests unsafe/name", "Body", null, null);

            Assert.Equal(PageSaveError.NameContainsUnsafeCharacters, result.Error);
            Assert.Null(result.Page);
            Assert.Null(await Repository.GetPageInfoByNavigation(TwNamespaceNavigation.CleanAndValidate("PageSaveServiceTests unsafe/name")));
        }

        [Fact]
        public async Task ANameWithTwoNamespaceSeparatorsIsRejected()
        {
            var result = await (await CreateService()).SavePage(0, 0, "PageSaveServiceTests :: Two :: Separators", "Body", null, null);

            Assert.Equal(PageSaveError.NameContainsMultipleNamespaceSeparators, result.Error);
            Assert.Null(result.Page);
        }

        [Fact]
        public async Task AMissingChangeSummaryIsRejectedWhenItIsRequired()
        {
            var configuration = fixture.Artifacts.Engine.WikiConfiguration;
            var showChangeSummary = configuration.ShowChangeSummaryWhenEditing;
            var requireChangeSummary = configuration.RequireChangeSummaryWhenEditing;
            try
            {
                configuration.ShowChangeSummaryWhenEditing = true;
                configuration.RequireChangeSummaryWhenEditing = true;

                var result = await (await CreateService()).SavePage(0, 0, "PageSaveServiceTests summary page", "Body", null, string.Empty);

                Assert.Equal(PageSaveError.ChangeSummaryRequired, result.Error);
                Assert.Null(await Repository.GetPageInfoByNavigation(TwNamespaceNavigation.CleanAndValidate("PageSaveServiceTests summary page")));
            }
            finally
            {
                configuration.ShowChangeSummaryWhenEditing = showChangeSummary;
                configuration.RequireChangeSummaryWhenEditing = requireChangeSummary;
            }
        }

        [Fact]
        public async Task ANewPageNeedsTheCreatePermission()
        {
            var service = await CreateService(o => o == TwPermission.Create);

            var result = await service.SavePage(0, 0, "PageSaveServiceTests no create", "Body", null, null);

            Assert.Equal(PageSaveError.CreateNotPermitted, result.Error);
            Assert.Null(await Repository.GetPageInfoByNavigation(TwNamespaceNavigation.CleanAndValidate("PageSaveServiceTests no create")));
        }

        [Fact]
        public async Task AnExistingPageNeedsTheEditPermission()
        {
            var existing = await CreateExistingPage("PageSaveServiceTests no edit", "The original body.");
            var service = await CreateService(o => o == TwPermission.Edit);

            var result = await service.SavePage(existing.Id, existing.Revision, existing.Name, "Changed body.", null, null);

            Assert.Equal(PageSaveError.EditNotPermitted, result.Error);
            Assert.Equal("The original body.", (await Repository.GetPageRevisionById(existing.Id))!.Body.Trim());
        }

        [Fact]
        public async Task AProtectedPageNeedsTheModeratePermission()
        {
            var existing = await CreateExistingPage("PageSaveServiceTests protected", "@@Protect(true)\r\nThe original body.");
            var denied = await CreateService(o => o == TwPermission.Moderate);

            var result = await denied.SavePage(existing.Id, existing.Revision, existing.Name, "Changed body.", null, null);

            Assert.Equal(PageSaveError.PageProtected, result.Error);
            Assert.Contains("The original body.", (await Repository.GetPageRevisionById(existing.Id))!.Body);

            var moderator = await (await CreateService()).SavePage(existing.Id, existing.Revision, existing.Name, "@@Protect(true)\r\nChanged body.", null, null);

            Assert.Equal(PageSaveError.None, moderator.Error);
        }

        [Fact]
        public async Task ANewPageWithAnExistingNavigationIsRejected()
        {
            await CreateExistingPage("PageSaveServiceTests duplicate", "The original body.");

            var result = await (await CreateService()).SavePage(0, 0, "PageSaveServiceTests duplicate", "Another body.", null, null);

            Assert.Equal(PageSaveError.NameAlreadyExists, result.Error);
            Assert.Null(result.Page);
        }

        [Fact]
        public async Task ARenameToAnExistingNavigationIsRejected()
        {
            var existing = await CreateExistingPage("PageSaveServiceTests rename source", "The source body.");
            await CreateExistingPage("PageSaveServiceTests rename target", "The target body.");

            var result = await (await CreateService()).SavePage(existing.Id, existing.Revision, "PageSaveServiceTests rename target", "Changed body.", null, null);

            Assert.Equal(PageSaveError.NameAlreadyExists, result.Error);
            var unchanged = await Repository.GetPageRevisionById(existing.Id);
            Assert.Equal(existing.Navigation, unchanged!.Navigation);
            Assert.Equal("The source body.", unchanged.Body.Trim());
        }

        [Fact]
        public async Task ASaveBasedOnAnOldRevisionIsAConflict()
        {
            var existing = await CreateExistingPage("PageSaveServiceTests conflict", "The first revision.");
            var service = await CreateService();
            var firstEdit = await service.SavePage(existing.Id, existing.Revision, existing.Name, "The second revision.", null, null);
            Assert.Equal(PageSaveError.None, firstEdit.Error);

            var result = await service.SavePage(existing.Id, existing.Revision, existing.Name, "Overwriting revision.", null, null);

            Assert.Equal(PageSaveError.RevisionConflict, result.Error);
            Assert.Equal("The second revision.", (await Repository.GetPageRevisionById(existing.Id))!.Body.Trim());
        }

        #endregion

        #region Saving.

        [Fact]
        public async Task ANewPageIsCreated()
        {
            var result = await (await CreateService()).SavePage(0, 0, "PageSaveServiceTests created", "The body.", "The description.", "The summary.");

            Assert.Equal(PageSaveError.None, result.Error);
            Assert.NotNull(result.Page);
            Assert.NotEqual(0, result.Page.Id);
            Assert.Equal(string.Empty, result.OriginalNavigation);
            var saved = await Repository.GetPageRevisionById(result.Page.Id);
            Assert.Equal("PageSaveServiceTests created", saved!.Name);
            Assert.Equal(TwNamespaceNavigation.CleanAndValidate("PageSaveServiceTests created"), saved.Navigation);
            Assert.Equal("The body.", saved.Body.Trim());
            Assert.Equal("The description.", saved.Description);
            Assert.Equal(await GetAuthorUserId(), saved.CreatedByUserId);
            var revisions = await Repository.GetPageRevisionsInfoByNavigationPaged(saved.Navigation, 1);
            Assert.Equal("The summary.", revisions.First().ChangeSummary);
        }

        [Fact]
        public async Task AnExistingPageGetsANewRevision()
        {
            var existing = await CreateExistingPage("PageSaveServiceTests edited", "The first revision.");

            var result = await (await CreateService()).SavePage(existing.Id, existing.Revision, existing.Name, "The second revision.", "The description.", "The summary.");

            Assert.Equal(PageSaveError.None, result.Error);
            Assert.NotNull(result.Page);
            Assert.Equal(existing.Id, result.Page.Id);
            Assert.Equal(string.Empty, result.OriginalNavigation);
            var saved = await Repository.GetPageRevisionById(existing.Id);
            Assert.Equal(existing.Revision + 1, saved!.Revision);
            Assert.Equal("The second revision.", saved.Body.Trim());
            var revisions = await Repository.GetPageRevisionsInfoByNavigationPaged(saved.Navigation, 1);
            Assert.Equal("The summary.", revisions.First().ChangeSummary);
        }

        [Fact]
        public async Task ARenamedPageReportsItsOriginalNavigation()
        {
            var existing = await CreateExistingPage("PageSaveServiceTests before rename", "The body.");
            //The repository hands out a cached page that the service changes, so the navigation is copied first.
            var originalNavigation = existing.Navigation;

            var result = await (await CreateService()).SavePage(existing.Id, existing.Revision, "PageSaveServiceTests after rename", "The body.", null, null);

            Assert.Equal(PageSaveError.None, result.Error);
            Assert.Equal(originalNavigation, result.OriginalNavigation);
            Assert.Equal(TwNamespaceNavigation.CleanAndValidate("PageSaveServiceTests after rename"), result.Page!.Navigation);
            Assert.Null(await Repository.GetPageInfoByNavigation(originalNavigation));
        }

        #endregion
    }
}
