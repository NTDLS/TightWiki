namespace TightWiki.Engine.Library.Interfaces
{
    public interface IInternalLinkHandler
    {
        public HandlerResult Handle(IWikifierSession wikifierSession, NamespaceNavigation pageNavigation, string pageName, string linkText, string? image, int imageScale);
    }
}
