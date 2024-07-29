namespace TightWiki.Engine.Library.Interfaces
{
    public interface IExceptionHandler
    {
        public void Log(IWikifierSession wikifier, Exception? ex, string customText);
    }
}
