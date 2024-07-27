namespace TightWiki.Engine.Library.Interfaces
{
    public interface ILinkHandler
    {
        public HandlerResult Handle(IWikifier wikifier, string link, string? text, string? image);
    }
}
