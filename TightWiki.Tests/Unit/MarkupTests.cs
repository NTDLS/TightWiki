using Microsoft.AspNetCore.Http;

namespace TightWiki.Tests.Unit
{
    [Collection("Markup Tests")]
    public class MarkupTests(TwEngineFixture fixture)
        : IClassFixture<TwEngineFixture>
    {

        [Theory]
        [InlineData("**bold**", "<strong>bold</strong>")]
        [InlineData("//italic//", "<i>italic</i>")]
        [InlineData("__underline__", "<u>underline</u>")]
        public async Task BasicMarkup_RendersCorrectly(string input, string expected)
        {
            var session = fixture.CreateWikiSession();
            var result = await fixture.Artifacts.Engine.Transform(fixture.Artifacts.Localizer, session, input);
            Assert.Contains(expected, result.HtmlResult);
        }
    }
}
