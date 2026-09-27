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
        [InlineData("==upsize1", "<div class=\"tw-heading tw-heading-1\" id=\"Toc_Tw73C1768B_0\"><a href=\"#Toc_Tw73C1768B_0\">upsize1</a></div>")]
        [InlineData("===upsize2", "<div class=\"tw-heading tw-heading-2\" id=\"Toc_Tw73C1768B_0\"><a href=\"#Toc_Tw73C1768B_0\">upsize2</a></div>")]
        [InlineData("====upsize3", "<div class=\"tw-heading tw-heading-3\" id=\"Toc_Tw73C1768B_0\"><a href=\"#Toc_Tw73C1768B_0\">upsize3</a></div>")]
        [InlineData("=====upsize4", "<div class=\"tw-heading tw-heading-4\" id=\"Toc_Tw73C1768B_0\"><a href=\"#Toc_Tw73C1768B_0\">upsize4</a></div>")]
        [InlineData("======upsize5", "<div class=\"tw-heading tw-heading-5\" id=\"Toc_Tw73C1768B_0\"><a href=\"#Toc_Tw73C1768B_0\">upsize5</a></div>")]
        [InlineData("=======upsize6", "<div class=\"tw-heading tw-heading-6\" id=\"Toc_Tw73C1768B_0\"><a href=\"#Toc_Tw73C1768B_0\">upsize6</a></div>")]
        [InlineData("========upsize7", "<div class=\"tw-heading tw-heading-6\" id=\"Toc_Tw73C1768B_0\"><a href=\"#Toc_Tw73C1768B_0\">upsize7</a></div>")]
        [InlineData("==Top\r\ntest1\r\n==Middle\r\nMiddle\r\n===Bottom\r\nBottom", "<div class=\"tw-heading tw-heading-1\" id=\"Toc_Tw73C1768B_2\"><a href=\"#Toc_Tw73C1768B_2\">Top</a></div><br />test1<br /><div class=\"tw-heading tw-heading-1\" id=\"Toc_Tw73C1768B_1\"><a href=\"#Toc_Tw73C1768B_1\">Middle</a></div><br />Middle<br /><div class=\"tw-heading tw-heading-2\" id=\"Toc_Tw73C1768B_0\"><a href=\"#Toc_Tw73C1768B_0\">Bottom</a></div><br />Bottom")]
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

        [Theory]
        //External links.
        [InlineData("[[https://example.com]]", "<a href=\"https://example.com\">https://example.com</a>")]
        [InlineData("[[https://example.com, Text]]", "<a href=\"https://example.com\">Text</a>")]
        [InlineData("[[https://example.com, Text, target:blank]]", "<a href=\"https://example.com\" target=\"_blank\" rel=\"noopener noreferrer\">Text</a>")]
        [InlineData("[[https://example.com, Text, target:_blank]]", "<a href=\"https://example.com\" target=\"_blank\" rel=\"noopener noreferrer\">Text</a>")]
        [InlineData("[[https://example.com, image:https://example.com/i.png]]", "<a href=\"https://example.com\"><img src=\"https://example.com/i.png\" border =\"0\"></a>")]
        [InlineData("[[https://example.com, image:https://example.com/i.png, 50, target:blank]]", "<a href=\"https://example.com\" target=\"_blank\" rel=\"noopener noreferrer\"><img src=\"https://example.com/i.png\" border =\"0\"></a>")]
        //Internal links.
        [InlineData("[[Home]]", "<a href=\"/home\">Home</a>")]
        [InlineData("[[Home, Text]]", "<a href=\"/home\">Text</a>")]
        [InlineData("[[Home, Text, 50]]", "<a href=\"/home\">Text</a>")]
        [InlineData("[[Home, Text, target:blank]]", "<a href=\"/home\" target=\"_blank\" rel=\"noopener noreferrer\">Text</a>")]
        [InlineData("[[Home, image:http://example.com/x.gif]]", "<a href=\"/home\"><img src=\"http://example.com/x.gif\" /></a>")]
        [InlineData("[[Home, image:Builtin :: Media/TightWiki Icon 6.png]]", "<a href=\"/home\"><img src=\"/Page/Image/Builtin :: Media/TightWiki Icon 6.png?Scale=100\" /></a>")]
        [InlineData("[[Home, image:Builtin :: Media/TightWiki Icon 6.png, 25]]", "<a href=\"/home\"><img src=\"/Page/Image/Builtin :: Media/TightWiki Icon 6.png?Scale=25\" /></a>")]
        [InlineData("[[Home, image:Builtin :: Media/TightWiki Icon 6.png, target:blank]]", "<a href=\"/home\" target=\"_blank\" rel=\"noopener noreferrer\"><img src=\"/Page/Image/Builtin :: Media/TightWiki Icon 6.png?Scale=100\" /></a>")]
        [InlineData("[[Home, image:Builtin :: Media/TightWiki Icon 6.png, 25, target:blank]]", "<a href=\"/home\" target=\"_blank\" rel=\"noopener noreferrer\"><img src=\"/Page/Image/Builtin :: Media/TightWiki Icon 6.png?Scale=25\" /></a>")]
        //Invalid options.
        [InlineData("[[Home, Text, bogus]]", "unknown argument \"bogus\"")]
        [InlineData("[[Home, Text, target:blank, target:self]]", "target specified more than once")]
        [InlineData("[[https://example.com, Text, target:]]", "invalid target \"target:\"")]
        public async Task LinkMarkup(string input, string expected)
        {
            var session = fixture.CreateWikiSession();
            var result = await fixture.Artifacts.Engine.Transform(fixture.Artifacts.Localizer, session, input);
            Assert.Contains(expected, result.HtmlResult);
        }

        [Theory]
        //Links on a page in a namespace resolve relative to that namespace, unless prefixed with "::" for the root namespace.
        [InlineData("[[::Home]]", "<a href=\"/home\">Home</a>")]
        [InlineData("[[::Home, Take me home!, target:blank]]", "<a href=\"/home\" target=\"_blank\" rel=\"noopener noreferrer\">Take me home!</a>")]
        [InlineData("[[Links]]", "<a href=\"/wiki_help::links\">Links</a>")]
        public async Task NamespacedLinkMarkup(string input, string expected)
        {
            var session = fixture.CreateWikiSession();
            var page = fixture.Artifacts.GetMockPage("Wiki Help :: Test", input);
            var result = await fixture.Artifacts.Engine.Transform(fixture.Artifacts.Localizer, session, page);
            Assert.Contains(expected, result.HtmlResult);
        }

        [Theory]
        //Plain tables, including the header/body structure.
        [InlineData("{{Table()\r\nA || B\r\n1 || 2\r\n}}", "<table class=\"table table-bordered\"><thead><tr><td><strong>A </strong></td><td><strong> B</strong></td></tr></thead><tbody><tr><td>1 </td><td> 2</td></tr></tbody></table>")]
        [InlineData("{{Table()\r\nA || B\r\n}}", "<table class=\"table table-bordered\"><thead><tr><td><strong>A </strong></td><td><strong> B</strong></td></tr></thead></table>")]
        [InlineData("{{Table(false, false)\r\nA || B\r\n}}", "<table class=\"table\"><tbody><tr><td>A </td><td> B</td></tr></tbody></table>")]
        [InlineData("{{StripedTable()\r\nA\r\n1\r\n}}", "<table class=\"table table-striped table-bordered\">")]
        //Column spans.
        [InlineData("{{Table()\r\nA || <\r\n1 || 2\r\n}}", "<thead><tr><td colspan=\"2\"><strong>A </strong></td></tr></thead><tbody><tr><td>1 </td><td> 2</td></tr></tbody>")]
        [InlineData("{{Table(true, false)\r\nA || < || <\r\n}}", "<tbody><tr><td colspan=\"3\">A </td></tr></tbody>")]
        //Row spans.
        [InlineData("{{Table()\r\nH1 || H2\r\nA || B\r\n^ || C\r\n}}", "<tbody><tr><td rowspan=\"2\">A </td><td> B</td></tr><tr><td> C</td></tr></tbody>")]
        //Column and row spans together.
        [InlineData("{{Table(true, false)\r\nA || < || B\r\n^ || ^ || C\r\n}}", "<tbody><tr><td colspan=\"2\" rowspan=\"2\">A </td><td> B</td></tr><tr><td> C</td></tr></tbody>")]
        [InlineData("{{Table(true, false)\r\nA || B\r\n^ || ^\r\n}}", "<tbody><tr><td rowspan=\"2\">A </td><td rowspan=\"2\"> B</td></tr><tr></tr></tbody>")]
        //Literal markers are not treated as merges.
        [InlineData("{{Table(true, false)\r\nA || #{<}#\r\n#{^}# || B\r\n}}", "<tbody><tr><td>A </td><td> &lt;</td></tr><tr><td>^ </td><td> B</td></tr></tbody>")]
        //Invalid merges.
        [InlineData("{{Table()\r\n< || A\r\n}}", "can not merge left from the first column")]
        [InlineData("{{Table(true, false)\r\n^ || A\r\n}}", "has no cell above it to merge into")]
        [InlineData("{{Table()\r\nA || B\r\n^ || C\r\n}}", "can not merge into the header row")]
        [InlineData("{{Table(true, false)\r\nA || B\r\nC || ^\r\n^ || <\r\n}}", "merged cells must form a rectangle")]
        public async Task TableMarkup(string input, string expected)
        {
            var session = fixture.CreateWikiSession();
            var result = await fixture.Artifacts.Engine.Transform(fixture.Artifacts.Localizer, session, input);
            Assert.Contains(expected, result.HtmlResult);
        }

        [Theory]
        [InlineData("@@HideSidebar\r\nText", "HideSidebar")]
        [InlineData("@@HideFooterComments\r\nText", "HideFooterComments")]
        [InlineData("@@HideFooterLastModified\r\nText", "HideFooterLastModified")]
        [InlineData("@@NoCache\r\nText", "NoCache")]
        public async Task ProcessingInstructionsAreRecordedAndNotRendered(string input, string instruction)
        {
            var session = fixture.CreateWikiSession();
            var result = await fixture.Artifacts.Engine.Transform(fixture.Artifacts.Localizer, session, input);
            Assert.Contains(instruction, result.ProcessingInstructions);
            Assert.Equal("Text", result.HtmlResult.Trim());
        }

        [Fact]
        public async Task CodeBlocksAreHtmlEncoded()
        {
            var session = fixture.CreateWikiSession();
            var result = await fixture.Artifacts.Engine.Transform(fixture.Artifacts.Localizer, session, "{{Code()\r\n<b>x</b> & \"y\"\r\n}}");
            Assert.Contains("<pre><code>&lt;b&gt;x&lt;/b&gt; &amp; &quot;y&quot;</code></pre>", result.HtmlResult);
            Assert.DoesNotContain("<b>x</b>", result.HtmlResult);
        }

        [Theory]
        [InlineData("{{Bullets\r\nA\r\n>B\r\n>>C\r\nD\r\n}}", "<ul><li>A</li><ul><li>B</li><ul><li>C</li></ul></ul><li>D</li></ul>")]
        [InlineData("{{Bullets(Ordered)\r\nA\r\nB\r\n}}", "<ol><li>A</li><li>B</li></ol>")]
        public async Task BulletMarkup(string input, string expected)
        {
            var session = fixture.CreateWikiSession();
            var result = await fixture.Artifacts.Engine.Transform(fixture.Artifacts.Localizer, session, input);
            Assert.Contains(expected, result.HtmlResult);
        }
    }
}
