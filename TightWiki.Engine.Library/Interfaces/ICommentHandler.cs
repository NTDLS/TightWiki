namespace TightWiki.Engine.Library.Interfaces
{
    public interface ICommentHandler
    {
        public HandlerResult Handle(IWikifierSession wikifierSession, string text);
    }
}
