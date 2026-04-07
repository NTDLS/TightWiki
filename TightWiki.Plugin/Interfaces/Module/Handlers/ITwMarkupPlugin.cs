using TightWiki.Plugin.Engine;
//TODO: Delete me
namespace TightWiki.Plugin.Interfaces.Module.Handlers
{
    /// <summary>
    /// Handles basic markup/style instructions like bold, italic, underline, etc.
    /// </summary>
    public interface ITwMarkupPlugin
    {
        /// <summary>
        /// Handles basic markup instructions like bold, italic, underline, etc.
        /// </summary>
        public Task<TwPluginResult> Handle(ITwEngineState state, string match);
    }
}
