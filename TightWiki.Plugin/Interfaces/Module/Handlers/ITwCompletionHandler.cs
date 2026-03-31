using TightWiki.Plugin.Engine;

namespace TightWiki.Plugin.Interfaces.Module.Handlers
{
    /// <summary>
    /// Handles wiki completion events.
    /// </summary>
    public interface ITwCompletionHandler
        : Interfaces.ITwDisabiguation
    {
        /// <summary>
        /// Handles wiki completion events. Is called when the wiki processing competes for a given page.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        Task<TwPluginResult> Handle(ITwEngineState state);
    }
}
