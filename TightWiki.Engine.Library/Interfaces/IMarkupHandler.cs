namespace TightWiki.Engine.Library.Interfaces
{
    public interface IMarkupHandler
    {
        public HandlerResult Handle(IWikifierSession wikifierSession, char sequence, string scopeBody);
    }
}
