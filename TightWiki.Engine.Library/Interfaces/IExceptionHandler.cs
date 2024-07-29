namespace TightWiki.Engine.Library.Interfaces
{
    public interface IExceptionHandler
    {
        public void Log(IWikifier wikifier, Exception? ex, string customText);
    }
}
