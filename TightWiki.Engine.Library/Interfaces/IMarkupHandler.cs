namespace TightWiki.Engine.Library.Interfaces
{
    public interface IMarkupHandler
    {
        public HandlerResult Handle(IWikifier wikifier, char sequence, string scopeBody);
    }
}
