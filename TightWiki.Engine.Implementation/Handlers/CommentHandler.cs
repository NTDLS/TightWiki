using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces;

namespace TightWiki.Engine.Implementation.Handlers
{
    /// <summary>
    /// Handles wiki comments. These are generally removed from the result.
    /// </summary>
    public class CommentHandler
        : ITwCommentHandler
    {
        /// <summary>
        /// Handles a wiki comment.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="text">The comment text</param>
        public async Task<TwHandlerResult> Handle(ITwEngineState state, string text)
        {
            return new TwHandlerResult() { Instructions = [HandlerResultInstruction.TruncateTrailingLine] };
        }
    }
}
