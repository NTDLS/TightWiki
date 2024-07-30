namespace TightWiki.Engine.Library.Interfaces
{
    public interface ICommentHandler
    {
        public HandlerResult Handle(ITightEngineState state, string text);
    }
}
