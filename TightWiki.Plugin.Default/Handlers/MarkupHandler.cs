using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Attributes.Functions;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Handlers;

namespace TightWiki.Plugin.Default.Handlers
{
    /// <summary>
    /// Handles basic markup/style instructions like bold, italic, underline, etc.
    /// </summary>
    [TwPluginModule("Default markup handler", "Handles basic markup instructions like bold, italic, underline, etc.", 1000)]
    public class MarkupHandler
        : ITwMarkupHandler
    {
        /// <summary>
        /// Handles basic markup instructions like bold, italic, underline, etc.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="sequence">The sequence of symbols that were found to denotate this markup instruction,</param>
        /// <param name="scopeBody">The body of text to apply the style to.</param>
        [TwMarkupHandler("Default markup handler", "Handles basic markup instructions like bold, italic, underline, etc.")]
        public async Task<TwHandlerResult> Handle(ITwEngineState state, char sequence, string scopeBody)
        {
            switch (sequence)
            {
                case '~': return new TwHandlerResult($"<strike>{scopeBody}</strike>");
                case '*': return new TwHandlerResult($"<strong>{scopeBody}</strong>");
                case '_': return new TwHandlerResult($"<u>{scopeBody}</u>");
                case '/': return new TwHandlerResult($"<i>{scopeBody}</i>");
                case '!': return new TwHandlerResult($"<mark>{scopeBody}</mark>");
                default:
                    break;
            }

            return new TwHandlerResult() { Instructions = [HandlerResultInstruction.Skip] };
        }
    }
}
