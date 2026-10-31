using TightWiki.Library;

namespace TightWiki.Tests.Unit
{
    public class SearchTextTests
    {
        [Theory]
        [InlineData("café", "cafe")]
        [InlineData("Crème Brûlée", "Creme Brulee")]
        [InlineData("señor", "senor")]
        [InlineData("Åland", "Aland")]
        [InlineData("straße", "strasse")]
        [InlineData("Søren", "Soren")]
        [InlineData("łódź", "lodz")]
        [InlineData("plain ascii", "plain ascii")]
        [InlineData("日本語", "日本語")]
        [InlineData("", "")]
        public void FoldDiacriticsRemovesAccents(string input, string expected)
        {
            Assert.Equal(expected, Utility.FoldDiacritics(input));
        }

        [Fact]
        public void FoldDiacriticsHandlesPrecomposedAndDecomposedInput()
        {
            Assert.Equal(Utility.FoldDiacritics("é"), Utility.FoldDiacritics("é"));
        }

        [Fact]
        public void FoldDiacriticsHandlesNull()
        {
            Assert.Equal(string.Empty, Utility.FoldDiacritics(null));
        }

        [Theory]
        [InlineData("wiki, pages.", "wiki|pages")]
        [InlineData("(hello) \"world\"!", "hello|world")]
        [InlineData("don't stop", "dont|stop")]
        [InlineData("snake_case-and/slash", "snake|case|and|slash")]
        [InlineData("Café au lait?", "Cafe|au|lait")]
        [InlineData("v2.40.1", "v2|40|1")]
        [InlineData("  ,,;;  ", "")]
        [InlineData("", "")]
        public void TokenizeTextKeepsOnlyLettersAndDigits(string input, string expected)
        {
            Assert.Equal(expected, string.Join('|', Utility.TokenizeText(input)));
        }

        [Fact]
        public void TokenizeTextHandlesNull()
        {
            Assert.Empty(Utility.TokenizeText(null));
        }
    }
}
