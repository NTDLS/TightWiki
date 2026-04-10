using TightWiki.Library;

namespace TightWiki.Tests.Unit
{
    [Collection("Full Page Tests")]
    public class FullPageTests(TwEngineFixture fixture)
        : IClassFixture<TwEngineFixture>
    {
        [Theory]
        [InlineData("TestInject")]
        [InlineData("TestInclude")]
        [InlineData("TestSnippet")]
        [InlineData("TestTags")]
        [InlineData("TestTags_1")]
        [InlineData("TestTags_2")]
        [InlineData("TestTagCloud")]
        [InlineData("TestSearchCloud")]
        [InlineData("TestTagList")]
        [InlineData("TestTagList_1")]
        [InlineData("TestSearchList")]
        [InlineData("TestNamespaceList")]
        [InlineData("TestNamespaceList_1")]
        [InlineData("TestNamespaceGlossary")]
        [InlineData("TestNamespaceGlossary_1")]
        [InlineData("TestTagGlossary")]
        [InlineData("TestTagGlossary_1")]
        //[InlineData("TestTextGlossary")]
        [InlineData("TestSystemEmojiList")]
        [InlineData("TestSystemEmojiCategoryList")]
        [InlineData("TestColor")]
        [InlineData("TestBR")]
        [InlineData("TestNL")]
        [InlineData("TestNewLine")]
        [InlineData("TestHR")]
        [InlineData("TestRevisions")]
        [InlineData("TestRecentlyModified")]
        [InlineData("TestRecentlyCreated")]
        [InlineData("TestMostEdited")]
        [InlineData("TestMostViewed")]
        [InlineData("TestAttachments")]
        [InlineData("TestImage")]
        [InlineData("TestFile")]
        [InlineData("TestLastModifiedBy")]
        [InlineData("TestCreatedBy")]
        [InlineData("TestPageRevisionCount")]
        [InlineData("TestPageviewCount")]
        [InlineData("TestPageCommentCount")]
        [InlineData("TestPageURL")]
        [InlineData("TestPageId")]
        [InlineData("TestLastModified")]
        [InlineData("TestCreated")]
        [InlineData("TestName")]
        [InlineData("TestTitle")]
        [InlineData("TestDescription")]
        [InlineData("TestNamespace")]
        [InlineData("TestSimilar")]
        [InlineData("TestRelated")]
        [InlineData("TestEditLink")]
        [InlineData("TestNavigation")]
        [InlineData("TestTOC")]
        [InlineData("TestSet")]
        [InlineData("TestGet")]
        [InlineData("TestSeq")]
        [InlineData("TestSiteName")]
        [InlineData("TestDotNetVersion")]
        [InlineData("TestAppVersion")]
        [InlineData("TestProfileGlossary")]
        [InlineData("TestProfileList")]

        public async Task TestPages(string input)
        {
            var testCase = GetTestCase(input);

            var session = fixture.CreateWikiSession();
            var page = fixture.Artifacts.GetMockPage("Test", testCase.Markup);
            var result = await fixture.Artifacts.Engine.Transform(fixture.Artifacts.Localizer, session, page);
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

