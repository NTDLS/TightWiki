namespace TightWiki.Engine.Library.Interfaces
{
    public interface IHeadingHandler
    {
        public HandlerResult Handle(ITightEngineState state, int depth, string link, string text);
    }
}
