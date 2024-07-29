namespace TightWiki.Engine.Library.Interfaces
{
    public interface IEmojiHandler
    {
        public HandlerResult Handle(IWikifierSession wikifierSession, string key, int scale);
    }
}
