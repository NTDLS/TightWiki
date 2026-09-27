using System.Text.RegularExpressions;

namespace TightWiki.Tests.Unit
{
    /// <summary>
    /// These assert invariants over the seed database rather than specific pages or dates, so they
    /// keep passing as the seed pages change.
    /// </summary>
    [Collection("Markup Tests")]
    public class PageRepositoryTests(TwEngineFixture fixture)
        : IClassFixture<TwEngineFixture>
    {
        [Fact]
        public async Task RecentlyModifiedIsNewestFirst()
        {
            var pages = await fixture.Artifacts.DatabaseManager.PageRepository.GetTopRecentlyModifiedPagesInfo(25);

            Assert.NotEmpty(pages);
            Assert.True(pages.Count <= 25);
            Assert.Equal(pages.OrderByDescending(o => o.ModifiedDate).Select(o => o.Id), pages.Select(o => o.Id));
        }

        [Fact]
        public async Task RecentlyModifiedUsesTheCurrentRevisionDate()
        {
            //https://github.com/NTDLS/TightWiki/issues/98 - the list must show the same date as the page footer.
            var repository = fixture.Artifacts.DatabaseManager.PageRepository;

            foreach (var page in await repository.GetTopRecentlyModifiedPagesInfo(10))
            {
                var current = await repository.GetPageRevisionByNavigation(page.Navigation);
                Assert.NotNull(current);
                Assert.Equal(current.ModifiedDate, page.ModifiedDate);
            }
        }

        [Fact]
        public async Task RecentlyModifiedLimitSelectsTheNewestPages()
        {
            //The LIMIT must be applied to the same date that is displayed, or the wrong pages make the cut.
            var repository = fixture.Artifacts.DatabaseManager.PageRepository;

            var top = await repository.GetTopRecentlyModifiedPagesInfo(5);
            var all = await repository.GetTopRecentlyModifiedPagesInfo(10000);

            Assert.Equal(all.Take(5).Select(o => o.ModifiedDate), top.Select(o => o.ModifiedDate));
        }

        [Fact]
        public async Task RecentlyCreatedIsNewestFirst()
        {
            var repository = fixture.Artifacts.DatabaseManager.PageRepository;

            var top = await repository.GetTopRecentlyCreatedPagesInfo(5);
            var all = await repository.GetTopRecentlyCreatedPagesInfo(10000);

            Assert.NotEmpty(top);
            Assert.Equal(top.OrderByDescending(o => o.CreatedDate).Select(o => o.Id), top.Select(o => o.Id));
            Assert.Equal(all.Max(o => o.CreatedDate), top[0].CreatedDate);
        }

        [Fact]
        public async Task RecentlyCreatedFunctionListsTheNewestCreatedPages()
        {
            //##RecentlyCreated used to query the recently modified pages.
            var expected = (await fixture.Artifacts.DatabaseManager.PageRepository.GetTopRecentlyCreatedPagesInfo(3))
                .OrderByDescending(o => o.CreatedDate).ThenBy(o => o.Title).Select(o => o.Title).ToList();

            var result = await fixture.Artifacts.Engine.Transform(fixture.Artifacts.Localizer, fixture.CreateWikiSession(), "##RecentlyCreated(3)");

            var rendered = Regex.Matches(result.HtmlResult, "<span class=\"fw-semibold text-truncate\">(.*?)</span>")
                .Select(o => o.Groups[1].Value).ToList();

            Assert.Equal(expected, rendered);
        }
    }
}
