namespace TightWiki.Engine.Library.Interfaces
{
    public interface IEmojiHandler
    {
        public HandlerResult Handle(IWikifier wikifier, string key, int scale);
    }
}
