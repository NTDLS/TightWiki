using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Attributes.Functions;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Handlers;

namespace TightWiki.Plugin.Default.Handlers
{
    /// <summary>
    /// Handles wiki comments. These are generally removed from the result.
    /// </summary>
    [TwPluginModule("Default comment handler", "Handles wiki comments.", 1000)]
    public class CommentHandler
        : ITwCommentHandler
    {
        /// <summary>
        /// Handles a wiki comment.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="text">The comment text</param>
        [TwCommentHandler("Default comment handler", "Handles wiki comments.")]
        public async Task<TwHandlerResult> Handle(ITwEngineState state, string text)
        {
            return new TwHandlerResult() { Instructions = [HandlerResultInstruction.TruncateTrailingLine] };
        }
    }
}
