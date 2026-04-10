namespace TightWiki.Tests.Unit
{
    [Collection("Markup Tests")]
    public class MarkupTests(TwEngineFixture fixture)
        : IClassFixture<TwEngineFixture>
    {
        [Theory]
        [InlineData("~~strike~~", "<strike>strike</strike>")]
        [InlineData("**bold**", "<strong>bold</strong>")]
        [InlineData("//italic//", "<i>italic</i>")]
        [InlineData("__underline__", "<u>underline</u>")]
        [InlineData("!!highlight!!", "<mark>highlight</mark>")]
        public async Task BasicMarkup(string input, string expected)
        {
            var session = fixture.CreateWikiSession();
            var result = await fixture.Artifacts.Engine.Transform(fixture.Artifacts.Localizer, session, input);
            Assert.Contains(expected, result.HtmlResult);
        }

        [Theory]
        [InlineData("^^upsize1", "<span class=\"mb-0\" style=\"font-size: 1.2rem;\">upsize1</span><br />")]
        [InlineData("^^^upsize2", "<span class=\"mb-0\" style=\"font-size: 1.4rem;\">upsize2</span><br />")]
        [InlineData("^^^^upsize3", "<span class=\"mb-0\" style=\"font-size: 1.6rem;\">upsize3</span><br />")]
        [InlineData("^^^^^upsize4", "<span class=\"mb-0\" style=\"font-size: 1.8rem;\">upsize4</span><br />")]
        [InlineData("^^^^^^upsize5", "<span class=\"mb-0\" style=\"font-size: 2.0rem;\">upsize5</span><br />")]
        [InlineData("^^^^^^^upsize6", "<span class=\"mb-0\" style=\"font-size: 2.2rem;\">upsize6</span><br />")]
        [InlineData("^^^^^^^^upsize7", "<span class=\"mb-0\" style=\"font-size: 2.4rem;\">upsize7</span><br />")]
        public async Task UpsizeMarkup(string input, string expected)
        {
            var session = fixture.CreateWikiSession();
            var result = await fixture.Artifacts.Engine.Transform(fixture.Artifacts.Localizer, session, input);
            Assert.Contains(expected, result.HtmlResult);
        }

        [Theory]
        [InlineData("Left#{the \"literal\" value}#right", "Leftthe &quot;literal&quot; valueright")]
        [InlineData("Left #{the \"literal\" value}# right", "Left the &quot;literal&quot; value right")]
        public async Task LiteralMarkup(string input, string expected)
        {
            var session = fixture.CreateWikiSession();
            var result = await fixture.Artifacts.Engine.Transform(fixture.Artifacts.Localizer, session, input);
            Assert.Contains(expected, result.HtmlResult);
        }

        [Theory]
        [InlineData("Top user text\r\n;;A wiki comment\r\nBottom user text", "Top user text<br />Bottom user text")]
        [InlineData("Top user text;;A wiki comment\r\nBottom user text", "Top user textBottom user text")]
        public async Task CommentMarkup(string input, string expected)
        {
            var session = fixture.CreateWikiSession();
            var result = await fixture.Artifacts.Engine.Transform(fixture.Artifacts.Localizer, session, input);
            Assert.Contains(expected, result.HtmlResult);
        }

        [Theory]
        [InlineData("==upsize1", "<div class=\"tw-heading tw-heading-1\" id=\"TOC_73C1768B_0\"><a href=\"#TOC_73C1768B_0\">upsize1</a></div>")]
        [InlineData("===upsize2", "<div class=\"tw-heading tw-heading-2\" id=\"TOC_73C1768B_0\"><a href=\"#TOC_73C1768B_0\">upsize2</a></div>")]
        [InlineData("====upsize3", "<div class=\"tw-heading tw-heading-3\" id=\"TOC_73C1768B_0\"><a href=\"#TOC_73C1768B_0\">upsize3</a></div>")]
        [InlineData("=====upsize4", "<div class=\"tw-heading tw-heading-4\" id=\"TOC_73C1768B_0\"><a href=\"#TOC_73C1768B_0\">upsize4</a></div>")]
        [InlineData("======upsize5", "<div class=\"tw-heading tw-heading-5\" id=\"TOC_73C1768B_0\"><a href=\"#TOC_73C1768B_0\">upsize5</a></div>")]
        [InlineData("=======upsize6", "<div class=\"tw-heading tw-heading-6\" id=\"TOC_73C1768B_0\"><a href=\"#TOC_73C1768B_0\">upsize6</a></div>")]
        [InlineData("========upsize7", "<div class=\"tw-heading tw-heading-6\" id=\"TOC_73C1768B_0\"><a href=\"#TOC_73C1768B_0\">upsize7</a></div>")]
        [InlineData("==Top\r\ntest1\r\n==Middle\r\nMiddle\r\n===Bottom\r\nBottom", "<div class=\"tw-heading tw-heading-1\" id=\"TOC_73C1768B_2\"><a href=\"#TOC_73C1768B_2\">Top</a></div><br />test1<br /><div class=\"tw-heading tw-heading-1\" id=\"TOC_73C1768B_1\"><a href=\"#TOC_73C1768B_1\">Middle</a></div><br />Middle<br /><div class=\"tw-heading tw-heading-2\" id=\"TOC_73C1768B_0\"><a href=\"#TOC_73C1768B_0\">Bottom</a></div><br />Bottom")]
        public async Task HeadingMarkup(string input, string expected)
        {
            var session = fixture.CreateWikiSession();
            var result = await fixture.Artifacts.Engine.Transform(fixture.Artifacts.Localizer, session, input);
            Assert.Contains(expected, result.HtmlResult);
        }

        [Theory]
        [InlineData("${testVar=Test Text}\r\n${testVar}", "Test Text")]
        [InlineData("${testVar=Test Text}\r\n**This is the**: ${testVar}", "<strong>This is the</strong>: Test Text")]
        public async Task VariableMarkup(string input, string expected)
        {
            var session = fixture.CreateWikiSession();
            var result = await fixture.Artifacts.Engine.Transform(fixture.Artifacts.Localizer, session, input);
            Assert.Contains(expected, result.HtmlResult);
        }

    }
}

