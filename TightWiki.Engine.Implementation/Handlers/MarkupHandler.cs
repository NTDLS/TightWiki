using TightWiki.Plugin;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;

namespace TightWiki.Engine.Implementation.Handlers
{
    /// <summary>
    /// Handles basic markup/style instructions like bole, italic, underline, etc.
    /// </summary>
    public class MarkupHandler
        : ITwMarkupHandler
    {
        /// <summary>
        /// Handles basic markup instructions like bole, italic, underline, etc.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="sequence">The sequence of symbols that were found to denotate this markup instruction,</param>
        /// <param name="scopeBody">The body of text to apply the style to.</param>
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
