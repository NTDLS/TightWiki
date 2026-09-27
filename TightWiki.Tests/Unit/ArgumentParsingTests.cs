using TightWiki.Plugin.Function;

namespace TightWiki.Tests.Unit
{
    public class ArgumentParsingTests
    {
        [Fact]
        public void SplitsOnCommasAndTrims()
        {
            Assert.Equal(["Home", "Take me home!", "target:blank"],
                ParsedFunction.ParseArgumentsAddParenthesis("Home,  Take me home! ,target:blank"));
        }

        [Fact]
        public void QuotedStringsKeepCommas()
        {
            Assert.Equal(["a, b", "c"], ParsedFunction.ParseArguments("(\"a, b\", c)"));
        }

        [Fact]
        public void EscapedQuotesStayInsideStrings()
        {
            Assert.Equal(["say \"hi\""], ParsedFunction.ParseArguments("(\"say \\\"hi\\\"\")"));
        }

        [Fact]
        public void NestedParenthesesStayInOneArgument()
        {
            //Note that commas are split on at any depth, so "(f(x, y), z)" yields three arguments.
            Assert.Equal(["f(x)", "z"], ParsedFunction.ParseArguments("(f(x), z)"));
        }

        [Fact]
        public void EmptyArgumentListIsEmpty()
        {
            Assert.Empty(ParsedFunction.ParseArguments("()"));
        }

        [Theory]
        [InlineData("a, b")]          //Missing the opening parenthesis.
        [InlineData("(\"unterminated)")]
        [InlineData("(a) trailing")]
        public void MalformedArgumentsThrow(string input)
        {
            Assert.ThrowsAny<Exception>(() => ParsedFunction.ParseArguments(input));
        }

        [Fact]
        public void AddParenthesisRejectsExplicitParentheses()
        {
            Assert.ThrowsAny<Exception>(() => ParsedFunction.ParseArgumentsAddParenthesis("(a, b)"));
        }
    }
}
