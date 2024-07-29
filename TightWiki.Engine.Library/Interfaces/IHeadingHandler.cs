namespace TightWiki.Engine.Library.Interfaces
{
    public interface IHeadingHandler
    {
        public HandlerResult Handle(IWikifierSession wikifierSession, int depth, string link, string text);
    }
}
