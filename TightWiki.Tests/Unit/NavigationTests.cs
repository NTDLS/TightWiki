using TightWiki.Plugin;

namespace TightWiki.Tests.Unit
{
    public class NavigationTests
    {
        [Theory]
        [InlineData("Home", "home")]
        [InlineData("  My Page  ", "my_page")]
        [InlineData("Version 2.0 Notes", "version_2_0_notes")]
        [InlineData("Tom & Jerry's \"Show\"", "tom_jerrys_show")]
        [InlineData("a   b", "a_b")]
        [InlineData("Docs\\Guide//Intro", "docs/guide/intro")]
        [InlineData("Fish &amp; Chips", "fish_chips")]
        [InlineData("::Page", "page")]
        [InlineData(null, "")]
        public void CleanNormalizesPageNames(string? input, string expected)
        {
            Assert.Equal(expected, TwNavigation.Clean(input));
        }

        [Fact]
        public void CleanRejectsNamespaces()
        {
            Assert.Throws<Exception>(() => TwNavigation.Clean("Wiki Help :: Links"));
        }

        [Theory]
        [InlineData("Wiki Help :: Links", "wiki_help::links")]
        [InlineData("Home", "home")]
        [InlineData("::Home", "home")]
        [InlineData("Namespace::", "namespace")]
        [InlineData("Docs/", "docs")]
        public void CleanAndValidateKeepsOneNamespace(string input, string expected)
        {
            Assert.Equal(expected, TwNamespaceNavigation.CleanAndValidate(input));
        }

        [Fact]
        public void CleanAndValidateCanPreservePageCase()
        {
            Assert.Equal("wiki_help::Links", TwNamespaceNavigation.CleanAndValidate("Wiki Help :: Links", lowerCase: false));
        }

        [Fact]
        public void CleanAndValidateRejectsNestedNamespaces()
        {
            Assert.Throws<Exception>(() => TwNamespaceNavigation.CleanAndValidate("A :: B :: C"));
        }

        [Fact]
        public void NamespaceNavigationSplitsCanonical()
        {
            var navigation = new TwNamespaceNavigation("Wiki Help :: Table");

            Assert.Equal("wiki_help", navigation.Namespace);
            Assert.Equal("table", navigation.Page);
            Assert.Equal("wiki_help::table", navigation.Canonical);
        }

        [Fact]
        public void NamespaceNavigationWithoutNamespaceIsJustThePage()
        {
            var navigation = new TwNamespaceNavigation("Home");

            Assert.Equal(string.Empty, navigation.Namespace);
            Assert.Equal("home", navigation.Canonical);
        }
    }
}
