using TightWiki.Library;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces.Repository;

namespace TightWiki.Tests.Unit
{
    /// <summary>
    /// Regression coverage for the same <see cref="DivideByZeroException"/> bug class documented on <see
    /// cref="PaginationEdgeCaseRegressionTests"/> (see that class's own remarks for the full background), found
    /// separately in <c>EfUsersRepository.GetAllPublicProfilesPaged</c> rather than <c>EfPageRepository</c>: its
    /// <c>PaginationPageCount</c> was computed as <c>(filtered.Count + (effectivePageSize - 1)) / effectivePageSize</c>
    /// with no guard against <c>effectivePageSize == 0</c>. Unlike every <c>EfPageRepository</c> member covered by
    /// <see cref="PaginationEdgeCaseRegressionTests"/>, this division runs entirely in-memory over an
    /// already-materialized <see cref="List{T}"/> (<c>GetAllAccountUserRowsAsync</c> - plain LINQ-to-Objects, no
    /// SQL translation for the division itself), so the bug is not merely provider-*independent* but reproduces
    /// identically regardless of which provider's rows were used to populate that list.
    /// <para>
    /// Reachable from real markup: <c>##ProfileList</c>/<c>##ProfileGlossary</c>
    /// (<c>TightWiki.Plugin.Default.StandardFunctions.UsersFunctions</c>) both expose <c>pageSize</c> as a plain
    /// <c>int</c> parameter a page author can type as <c>0</c> - exactly the seed markup behind
    /// <c>TightWiki.Tests/Markup/TestProfileList_000003</c>/<c>_000004</c> and
    /// <c>TestProfileGlossary_000003</c>/<c>_000004</c> (<c>##ProfileList(0)</c>/<c>##ProfileList(0, "test")</c> and
    /// the <c>##ProfileGlossary</c> equivalents), which fail on SqlServer/Postgres before the fix and pass after it.
    /// The SQLite/Dapper reference (<c>UsersRepository.GetAllPublicProfilesPaged</c>,
    /// <c>GetAllPublicProfilesPaged.sql</c>) never hits this, same "<c>LIMIT 0</c> returns zero rows before any
    /// division happens" reasoning as every scenario in <see cref="PaginationEdgeCaseRegressionTests"/>.
    /// </para>
    /// <para>
    /// Obtained through <see cref="TwEngineFixture"/> exactly like every sibling repository test class in this
    /// project; written entirely against the provider-agnostic <see cref="ITwUsersRepository"/> interface, so the
    /// same compiled test code runs against SQLite (default), SQL Server (<c>-p:DataProvider=SqlServer</c>), and
    /// Postgres (<c>-p:DataProvider=Postgres</c>). Joins the existing "Users Repository Profile Tests" xunit
    /// collection rather than defining a new one, same precedent as <c>DataIntegrityRegressionTests</c> joining
    /// "Users Repository Permission Auth Tests" - this class is strictly read-only against the shared, persistent
    /// seeded database (the real, already-seeded <c>admin</c> account, same as <see
    /// cref="UsersRepositoryProfileTests"/>), so no additional serialization risk beyond what that collection
    /// already provides.
    /// </para>
    /// </summary>
    [Collection("Users Repository Profile Tests")]
    public class UsersRepositoryPaginationEdgeCaseRegressionTests(TwEngineFixture fixture)
        : IClassFixture<TwEngineFixture>
    {
        [Fact]
        public async Task GetAllPublicProfilesPaged_PageSizeZero_ReturnsEmptyList_NotThrows()
        {
            var repo = fixture.Artifacts.DatabaseManager.UsersRepository;

            //Baseline: the seeded database always has at least one public profile (the real, seeded "admin"
            //account), so a real, positive pageSize must return a non-empty result - proves pageSize: 0 below
            //isn't "empty because nothing ever matched anyway".
            var baseline = await repo.GetAllPublicProfilesPaged(pageNumber: 1, pageSize: 1);
            Assert.NotEmpty(baseline);

            //Before the fix, this threw DivideByZeroException from (filtered.Count + (effectivePageSize - 1)) /
            //effectivePageSize; it must now return an empty list, matching the SQLite reference's own "LIMIT 0
            //always returns zero rows" behavior (GetAllPublicProfilesPaged.sql) - exactly the scenario
            //##ProfileList(0)/##ProfileGlossary(0) exercise (TestProfileList_000003/TestProfileGlossary_000003).
            var zeroPageSize = await repo.GetAllPublicProfilesPaged(pageNumber: 1, pageSize: 0);
            Assert.Empty(zeroPageSize);
        }

        [Fact]
        public async Task GetAllPublicProfilesPaged_PageSizeZero_WithSearchToken_ReturnsEmptyList_NotThrows()
        {
            var repo = fixture.Artifacts.DatabaseManager.UsersRepository;

            //"admin" alone is a unique enough substring of the seeded account's own AccountName
            //(Constants.DEFAULTACCOUNT) to guarantee a non-empty baseline at a real, positive pageSize - same
            //reasoning as UsersRepositoryProfileTests' own searchToken assertions.
            var baseline = await repo.GetAllPublicProfilesPaged(pageNumber: 1, pageSize: 1, searchToken: Constants.DEFAULTACCOUNT);
            Assert.NotEmpty(baseline);

            //Exercises the searchToken-filtered branch of GetAllPublicProfilesPaged at pageSize: 0 - exactly the
            //scenario ##ProfileList(0, "test")/##ProfileGlossary(0, "test") exercise
            //(TestProfileList_000004/TestProfileGlossary_000004).
            var zeroPageSize = await repo.GetAllPublicProfilesPaged(pageNumber: 1, pageSize: 0, searchToken: Constants.DEFAULTACCOUNT);
            Assert.Empty(zeroPageSize);
        }
    }
}
