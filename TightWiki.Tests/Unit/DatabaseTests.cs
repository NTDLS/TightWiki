namespace TightWiki.Tests.Unit
{
    [Collection("Database Tests")]
    public class DatabaseTests(/*TwEngineFixture fixture*/)
        : IClassFixture<TwEngineFixture>
    {
        /*
        [Fact(DisplayName = "Empty page statistics.")]
        public void EmptyPageStatistics()
        {
            var pageStatisticsRowsExists = fixture.Artifacts.DatabaseManager.StatisticsRepository.StatisticsFactory
                .ExecuteScalar<int>(@"Scripts\PageStatisticsRowsExists.sql");
            Assert.Equal(0, pageStatisticsRowsExists);
        }

        [Fact(DisplayName = "Empty event log.")]
        public void EmptyEventLog()
        {
            var pageStatisticsRowsExists = fixture.Artifacts.DatabaseManager.LoggingRepository.LoggingFactory
                .ExecuteScalar<int>(@"Scripts\EventLogRowsExists.sql");
            Assert.Equal(0, pageStatisticsRowsExists);
        }
        */

    }
}
