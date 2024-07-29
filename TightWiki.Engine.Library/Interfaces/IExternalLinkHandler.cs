namespace TightWiki.Engine.Library.Interfaces
{
    public interface IExternalLinkHandler
    {
        public HandlerResult Handle(IWikifier wikifier, string link, string? text, string? image);
    }
}
