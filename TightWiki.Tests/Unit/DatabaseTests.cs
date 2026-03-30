namespace TightWiki.Tests.Unit
{
    [Collection("Database Tests")]
    public class DatabaseTests(TwEngineFixture fixture)
        : IClassFixture<TwEngineFixture>
    {
        [Fact(DisplayName = "Empty page statistics.")]
        public void EmptyPageStatistics()
        {
            var pageStatisticsRowsExists = ManagedDataStorage.Statistics.ExecuteScalar<int>(@"Scripts\PageStatisticsRowsExists.sql");
            Assert.Equal(0, pageStatisticsRowsExists);
        }

        [Fact(DisplayName = "Empty event log.")]
        public void EmptyEventLog()
        {
            var pageStatisticsRowsExists = ManagedDataStorage.Logging.ExecuteScalar<int>(@"Scripts\EventLogRowsExists.sql");
            Assert.Equal(0, pageStatisticsRowsExists);
        }


        [Fact(DisplayName = "Engine test.")]
        public async Task EngineTest()
        {
            string filePath = @"C:\Users\jpatterson\Desktop\Tests\test.txt";

            string body;
            string given;

            given = "This is a **test**!";
            body = (await fixture.WikiTransform("Test :: Page 1", given)).HtmlResult;
            File.AppendAllText(filePath, $"{given}\r\n{body}\r\n");

            given = "This is a //test//!";
            body = (await fixture.WikiTransform("Test :: Page 1", given)).HtmlResult;
            File.AppendAllText(filePath, $"{given}\r\n{body}\r\n");

            given = "This is a __test__!";
            body = (await fixture.WikiTransform("Test :: Page 1", given)).HtmlResult;
            File.AppendAllText(filePath, $"{given}\r\n{body}\r\n");

            given = "This is a ~~test~~!";
            body = (await fixture.WikiTransform("Test :: Page 1", given)).HtmlResult;
            File.AppendAllText(filePath, $"{given}\r\n{body}\r\n");

            given = "This is a !!test!!!";
            body = (await fixture.WikiTransform("Test :: Page 1", given)).HtmlResult;
            File.AppendAllText(filePath, $"{given}\r\n{body}\r\n");

            //Can we loop here and test all the transformations in one go?

            //foreach (var prototype in fixture.EngineArtifacts.Engine.StandardFunctionHandler.Prototypes.Items)
            //{
            //}

            /*

            Assert.Equal("This is a <strong>test</strong>!",
                (await fixture.Transform("Test :: Page 1", "This is a **test**!")).HtmlResult);

            Assert.Equal("This is a <strong>test</strong>!",
                (await fixture.Transform("Test :: Page 1", "This is a **test**!")).HtmlResult);

            Assert.Equal("This is a <strong>test</strong>!",
                (await fixture.Transform("Test :: Page 1", "This is a **test**!")).HtmlResult);
            */
        }
    }
}
