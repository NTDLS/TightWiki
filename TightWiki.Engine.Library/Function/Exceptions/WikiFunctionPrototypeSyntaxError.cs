namespace TightWiki.Engine.Library.Function.Exceptions
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
