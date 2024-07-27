namespace TightWiki.Engine.Library.Interfaces
{
    public interface ICommentHandler
    {
        public HandlerResult Handle(IWikifier wikifier, string text);
    }
}
