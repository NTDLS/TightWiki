namespace TightWiki.Engine.Library.Interfaces
{
    public interface IHeadingHandler
    {
        public HandlerResult Handle(IWikifier wikifier, int depth, string link, string text);
    }
}
