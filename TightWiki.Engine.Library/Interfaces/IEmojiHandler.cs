namespace TightWiki.Engine.Library.Interfaces
{
    public interface IEmojiHandler
    {
        public HandlerResult Handle(ITightEngineState state, string key, int scale);
    }
}
