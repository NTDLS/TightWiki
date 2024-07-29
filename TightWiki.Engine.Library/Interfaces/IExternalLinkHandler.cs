namespace TightWiki.Engine.Library.Interfaces
{
    public interface IExternalLinkHandler
    {
        public HandlerResult Handle(IWikifierSession wikifierSession, string link, string? text, string? image);
    }
}
