namespace TightWiki.Engine.Library.Interfaces
{
    public interface IMarkupHandler
    {
        public HandlerResult Handle(ITightEngineState state, char sequence, string scopeBody);
    }
}
