using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Attributes.Handlers;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;

namespace TightWiki.Plugin.Default.Handlers
{
    /// <summary>
    /// Handles basic markup/style instructions like bold, italic, underline, etc.
    /// </summary>
    [TwPlugin("Default markup handler", "Handles basic markup instructions like bold, italic, underline, etc.", 1000)]
    public class MarkupHandler
    {
        /// <summary>
        /// Handles basic markup instructions like bold, italic, underline, etc.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="sequence">The sequence of symbols that were found to denotate this markup instruction,</param>
        /// <param name="scopeBody">The body of text to apply the style to.</param>
        [TwMarkupPluginHandler("Default markup handler", "Handles basic markup instructions like bold, italic, underline, etc.")]
        public async Task<TwPluginResult> Handle(ITwEngineState state, char sequence, string scopeBody)
        {
            switch (sequence)
            {
                case '~': return new TwPluginResult($"<strike>{scopeBody}</strike>");
                case '*': return new TwPluginResult($"<strong>{scopeBody}</strong>");
                case '_': return new TwPluginResult($"<u>{scopeBody}</u>");
                case '/': return new TwPluginResult($"<i>{scopeBody}</i>");
                case '!': return new TwPluginResult($"<mark>{scopeBody}</mark>");
                default:
                    break;
            }

            return new TwPluginResult() { Instructions = [HandlerResultInstruction.Skip] };
        }
    }
}
