namespace TightWiki.Engine.Library.Function.Exceptions
{
    public class WikiFunctionParserError : Exception
    {
        public WikiFunctionParserError()
        {
        }

        public WikiFunctionParserError(string message)
            : base(message)
        {
        }
    }
}
