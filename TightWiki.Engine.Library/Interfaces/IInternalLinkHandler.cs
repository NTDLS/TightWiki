namespace TightWiki.Engine.Library.Interfaces
{
    public interface IInternalLinkHandler
    {
        public HandlerResult Handle(ITightEngineState state, NamespaceNavigation pageNavigation, string pageName, string linkText, string? image, int imageScale);
    }
}
