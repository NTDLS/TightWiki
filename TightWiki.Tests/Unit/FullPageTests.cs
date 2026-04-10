namespace TightWiki.Tests.Unit
{
    [Collection("Full Page Tests")]
    public class FullPageTests(TwEngineFixture fixture)
        : IClassFixture<TwEngineFixture>
    {
        public static string MarkupPath = "..\\..\\..\\Markup";

        public static IEnumerable<object[]> FullPageTestCases()
        {
            foreach (var file in Directory.GetFiles(MarkupPath, "*.wiki"))
            {
                yield return new object[] { Path.GetFileNameWithoutExtension(file) };
            }
        }

        internal class PageTest
        {
            public string Markup { get; set; } = string.Empty;
            public string Expected { get; set; } = string.Empty;
        }

        private static PageTest GetTestCase(string fileNamePart)
        {
            var currentDirectory = Environment.CurrentDirectory;

            var markup = File.ReadAllText(Path.Combine(MarkupPath, $@"{fileNamePart}.wiki"));
            var expected = File.ReadAllText(Path.Combine(MarkupPath, $@"{fileNamePart}.wiki.expected"));

            return new PageTest
            {
                Markup = markup,
                Expected = expected
            };
        }

        [Theory]
        [MemberData(nameof(FullPageTestCases))]
        public async Task TestPages(string input)
        {
            var testCase = GetTestCase(input);

            var session = fixture.CreateWikiSession();
            var page = fixture.Artifacts.GetMockPage("Test", testCase.Markup);
            var result = await fixture.Artifacts.Engine.Transform(fixture.Artifacts.Localizer, session, page);
            Assert.Equal(testCase.Expected, result.HtmlResult);
        }
    }
}

