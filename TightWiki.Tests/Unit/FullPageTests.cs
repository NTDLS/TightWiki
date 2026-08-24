namespace TightWiki.Tests.Unit
{
    [Collection("Full Page Tests")]
    public class FullPageTests(TwEngineFixture fixture)
        : IClassFixture<TwEngineFixture>
    {
        public static string MarkupPath = Path.Combine("..", "..", "..", "Markup");

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

        /// <summary>
        /// Normalizes known ICU-version-dependent whitespace so that fixture comparisons are not sensitive to
        /// which ICU version produced the string. Specifically, DateTime.ToShortTimeString() (used by
        /// HistoryFunctions.cs/MetadataFunctions.cs) separates the time from the AM/PM designator with a plain
        /// space (U+0020) under the ICU version bundled on Windows (which is what the Markup/*.wiki.expected
        /// fixtures were generated with), but with a narrow no-break space (U+202F) under newer ICU versions -
        /// notably the one bundled with .NET on ubuntu-latest - per a CLDR formatting change. Both render
        /// visually identical but compare as different strings. This does not affect production code/output -
        /// real users see whichever (visually indistinguishable) character their runtime's ICU produces.
        /// </summary>
        private static string NormalizeIcuWhitespace(string value) => value.Replace('\u202F', '\u0020');

        [Theory]
        [MemberData(nameof(FullPageTestCases))]
        public async Task TestPages(string input)
        {
            var testCase = GetTestCase(input);

            var session = fixture.CreateWikiSession();
            var page = fixture.Artifacts.GetMockPage("Test", testCase.Markup);
            var result = await fixture.Artifacts.Engine.Transform(fixture.Artifacts.Localizer, session, page);

            var expected = NormalizeIcuWhitespace(testCase.Expected);
            var actual = NormalizeIcuWhitespace(result.HtmlResult);

            Assert.Equal(expected, actual);
        }
    }
}
