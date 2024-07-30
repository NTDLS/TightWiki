namespace TightWiki.Engine.Function.Exceptions
{
    public class WikiFunctionPrototypeSyntaxError : Exception
    {
        public WikiFunctionPrototypeSyntaxError()
        {
        }

        public WikiFunctionPrototypeSyntaxError(string message)
            : base(message)
        {
        }
    }
}
