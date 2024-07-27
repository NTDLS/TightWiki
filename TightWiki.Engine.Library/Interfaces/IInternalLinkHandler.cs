namespace TightWiki.Engine.Library.Interfaces
{
    public interface IInternalLinkHandler
    {
        public HandlerResult Handle(IWikifier wikifier, NamespaceNavigation pageNavigation, string pageName, string linkText, string? image, int imageScale);
    }
}
