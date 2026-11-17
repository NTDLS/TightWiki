using System.Reflection;
using TightWiki.Library;
using TightWiki.Library.Api;
using TightWiki.Library.Dummy;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;
using TwPermission = TightWiki.Plugin.TwPermission;

namespace TightWiki.Tests.Unit
{
    [Collection("Markup Tests")]
    public class ApiWriteServiceTests(TwEngineFixture fixture)
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

        private async Task<Guid> GetAuthorUserId()
        {
            foreach (var name in await Repository.GetAllNamespaces())
            {
                if (string.IsNullOrEmpty(name) == false)
                {
                    var pages = await Repository.GetPageInfoByNamespaces([name]);
                    var author = await Repository.GetPageRevisionByNavigation(pages.First().Navigation);
                    return author!.ModifiedByUserId;
                }
            }
            throw new Exception("The seed database has no namespaces.");
        }

        private async Task<ApiWriteService> CreateService(Func<TwPermission, bool> denyWhen, string source)
        {
            var inner = new TwDummySessionState()
            {
                Profile = new TwAccountProfile() { UserId = await GetAuthorUserId() }
            };
            var session = DispatchProxy.Create<ITwSessionState, SelectiveSession>();
            var proxy = (SelectiveSession)(object)session;
            proxy.Inner = inner;
            proxy.DenyWhen = denyWhen;

            return new ApiWriteService(session, fixture.Artifacts.Engine, fixture.Artifacts.Localizer, source);
        }

        private Task<ApiWriteService> CreateService() => CreateService(_ => false, ApiWriteService.ApiSource);

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

        private async Task<string> GetLatestChangeSummary(string navigation)
        {
            var revisions = await Repository.GetPageRevisionsInfoByNavigationPaged(navigation, 1);
            return revisions.OrderByDescending(o => o.Revision).First().ChangeSummary;
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ACreateWithoutAChangeSummaryIsRejected(string? changeSummary)
        {
            var result = await (await CreateService()).CreatePage("ApiWriteServiceTests no summary", "Body", null, changeSummary);

            Assert.Equal(PageSaveError.ChangeSummaryRequired, result.Error);
            Assert.Null(await Repository.GetPageInfoByNavigation(TwNamespaceNavigation.CleanAndValidate("ApiWriteServiceTests no summary")));
        }

        [Fact]
        public async Task AnUpdateWithoutAChangeSummaryIsRejected()
        {
            var existing = await CreateExistingPage("ApiWriteServiceTests update no summary", "The original body.");

            var result = await (await CreateService()).UpdatePage(existing.Navigation, existing.Revision, "Changed body.", null, " ");

            Assert.Equal(PageSaveError.ChangeSummaryRequired, result!.Error);
            Assert.Equal("The original body.", (await Repository.GetPageRevisionById(existing.Id))!.Body.Trim());
        }

        [Theory]
        [InlineData(ApiWriteService.ApiSource, "[API] Added a page.")]
        [InlineData(ApiWriteService.McpSource, "[MCP] Added a page.")]
        public async Task ACreatedRevisionIsMarkedWithTheSource(string source, string expectedSummary)
        {
            var service = await CreateService(_ => false, source);

            var result = await service.CreatePage("ApiWriteServiceTests created " + source, "The body.", null, "Added a page.");

            Assert.Equal(PageSaveError.None, result.Error);
            Assert.Equal(expectedSummary, await GetLatestChangeSummary(result.Page!.Navigation));
        }

        [Fact]
        public async Task AnUpdatedRevisionIsMarkedAndKeepsTheName()
        {
            var existing = await CreateExistingPage("ApiWriteServiceTests updated", "The first revision.");

            var result = await (await CreateService()).UpdatePage(existing.Navigation, existing.Revision, "The second revision.", null, "Changed it.");

            Assert.Equal(PageSaveError.None, result!.Error);
            Assert.Equal(existing.Revision + 1, result.Page!.Revision);
            Assert.Equal("ApiWriteServiceTests updated", result.Page.Name);
            Assert.Equal("[API] Changed it.", await GetLatestChangeSummary(result.Page.Navigation));
        }

        [Fact]
        public async Task ANewPageNeedsTheCreatePermission()
        {
            var service = await CreateService(o => o == TwPermission.Create, ApiWriteService.ApiSource);

            var result = await service.CreatePage("ApiWriteServiceTests no create", "Body", null, "Added a page.");

            Assert.Equal(PageSaveError.CreateNotPermitted, result.Error);
            Assert.Null(await Repository.GetPageInfoByNavigation(TwNamespaceNavigation.CleanAndValidate("ApiWriteServiceTests no create")));
        }

        [Fact]
        public async Task AnExistingPageNeedsTheEditPermission()
        {
            var existing = await CreateExistingPage("ApiWriteServiceTests no edit", "The original body.");
            var service = await CreateService(o => o == TwPermission.Edit, ApiWriteService.ApiSource);

            var result = await service.UpdatePage(existing.Navigation, existing.Revision, "Changed body.", null, "Changed it.");

            Assert.Equal(PageSaveError.EditNotPermitted, result!.Error);
            Assert.Equal("The original body.", (await Repository.GetPageRevisionById(existing.Id))!.Body.Trim());
        }

        [Fact]
        public async Task AProtectedPageNeedsTheModeratePermission()
        {
            var existing = await CreateExistingPage("ApiWriteServiceTests protected", "@@Protect(true)\r\nThe original body.");
            var denied = await CreateService(o => o == TwPermission.Moderate, ApiWriteService.ApiSource);

            var result = await denied.UpdatePage(existing.Navigation, existing.Revision, "Changed body.", null, "Changed it.");

            Assert.Equal(PageSaveError.PageProtected, result!.Error);

            var moderator = await (await CreateService()).UpdatePage(existing.Navigation, existing.Revision, "@@Protect(true)\r\nChanged body.", null, "Changed it.");

            Assert.Equal(PageSaveError.None, moderator!.Error);
        }

        [Fact]
        public async Task AnEditBasedOnAStaleRevisionIsAConflictAndReturnsTheCurrentRevision()
        {
            var existing = await CreateExistingPage("ApiWriteServiceTests conflict", "The first revision.");
            var service = await CreateService();
            var firstEdit = await service.UpdatePage(existing.Navigation, existing.Revision, "The second revision.", null, "First edit.");
            Assert.Equal(PageSaveError.None, firstEdit!.Error);

            var result = await service.UpdatePage(existing.Navigation, existing.Revision, "The stale edit.", null, "Stale edit.");

            Assert.Equal(PageSaveError.RevisionConflict, result!.Error);
            Assert.Equal(firstEdit.Page!.Revision, result.Page!.Revision);
            Assert.Equal("The second revision.", (await Repository.GetPageRevisionById(existing.Id))!.Body.Trim());
        }

        [Fact]
        public async Task AMissingPageWithoutTheEditPermissionIsNotRevealed()
        {
            var service = await CreateService(o => o == TwPermission.Edit, ApiWriteService.ApiSource);

            var result = await service.UpdatePage("ApiWriteServiceTests missing hidden", 1, "Body", null, "Changed it.");

            Assert.Equal(PageSaveError.EditNotPermitted, result!.Error);
        }

        [Fact]
        public async Task AnUpdateKeepsTheDescriptionWhenItIsNull()
        {
            var service = await CreateService();
            var created = await service.CreatePage("ApiWriteServiceTests description", "The body.", "The description.", "Added a page.");

            var kept = await service.UpdatePage(created.Page!.Navigation, created.Page.Revision, "Changed body.", null, "Changed it.");
            Assert.Equal("The description.", kept!.Page!.Description);

            var cleared = await service.UpdatePage(created.Page.Navigation, kept.Page.Revision, "Changed again.", "", "Cleared it.");
            Assert.Equal(string.Empty, cleared!.Page!.Description);
        }

        [Fact]
        public async Task AnUpdateOfANavigationWithMoreThanOneNamespaceIsRejected()
        {
            var result = await (await CreateService()).UpdatePage("one::two::three", 1, "Body", null, "Changed it.");

            Assert.Equal(PageSaveError.NameContainsMultipleNamespaceSeparators, result!.Error);
        }

        [Fact]
        public async Task AMissingPageIsNotUpdated()
        {
            var result = await (await CreateService()).UpdatePage("ApiWriteServiceTests missing", 1, "Body", null, "Changed it.");

            Assert.Null(result);
        }
    }
}
