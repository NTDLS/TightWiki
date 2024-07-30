using TightWiki.Engine.Library;
using TightWiki.Engine.Library.Interfaces;
using static TightWiki.Engine.Library.Constants;

namespace TightWiki.Engine.Implementation
{
    /// <summary>
    /// Handles wiki comments. These are generally removed from the result.
    /// </summary>
    public class CommentHandler : ICommentHandler
    {
        /// <summary>
        /// Handles a wiki comment.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="text">The comment text</param>
        public HandlerResult Handle(ITightEngineState state, string text)
        {
            return new HandlerResult() { Instructions = [HandlerResultInstruction.TruncateTrailingLine] };
        }
    }
}
