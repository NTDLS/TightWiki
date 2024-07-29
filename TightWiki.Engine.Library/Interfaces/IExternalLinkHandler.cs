namespace TightWiki.Engine.Library.Interfaces
{
    public interface IExternalLinkHandler
    {
        public HandlerResult Handle(ITightEngineState state, string link, string? text, string? image);
    }
}
