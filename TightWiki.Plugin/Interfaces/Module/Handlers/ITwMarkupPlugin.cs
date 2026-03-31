using TightWiki.Plugin.Engine;

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
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="sequence">The sequence of symbols that were found to denotate this markup instruction,</param>
        /// <param name="scopeBody">The body of text to apply the style to.</param>
        public Task<TwPluginResult> Handle(ITwEngineState state, char sequence, string scopeBody);
    }
}
