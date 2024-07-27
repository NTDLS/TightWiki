namespace TightWiki.Engine.Function.Exceptions
{
    public class WikiFunctionPrototypeNotDefinedException: Exception
    {
        public WikiFunctionPrototypeNotDefinedException()
        {
        }

        public WikiFunctionPrototypeNotDefinedException(string message)
            : base(message)
        {
           
        }
    }
}
