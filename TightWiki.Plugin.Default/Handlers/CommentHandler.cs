using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Attributes.Handlers;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Module.Handlers;

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
        public async Task<TwPluginResult> Handle(ITwEngineState state, string text)
        {
            return new TwPluginResult() { Instructions = [HandlerResultInstruction.TruncateTrailingLine] };
        }
    }
}
