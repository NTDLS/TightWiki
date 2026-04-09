using TightWiki.Library;

namespace TightWiki.Tests.Unit
{
    [Collection("Full Page Tests")]
    public class FullPageTests(TwEngineFixture fixture)
        : IClassFixture<TwEngineFixture>
    {
        [Fact(DisplayName = "Test pages.")]
        public async Task TestPages()
        {
            var testCase = GetTestCase("Test");

            var session = fixture.CreateWikiSession();
            var result = await fixture.Artifacts.Engine.Transform(fixture.Artifacts.Localizer, session, testCase.Markup);
            Assert.Equal(testCase.Expected, result.HtmlResult);
        }

        class PageTest
        {
            public string Markup { get; set; } = string.Empty;
            public string Expected { get; set; } = string.Empty;
        }

        private static PageTest GetTestCase(string fileNamePart)
        {
            return new PageTest
            {
                Markup = EmbeddedResourceReader.LoadText($@"Markup\{fileNamePart}.wiki"),
                Expected = EmbeddedResourceReader.LoadText($@"Markup\{fileNamePart}.wiki.expected")
            };
        }
    }
}

