using TightWiki.Engine.Library;
using TightWiki.Engine.Library.Interfaces;
using static TightWiki.Engine.Library.Constants;

namespace TightWiki.Engine.Handlers
{
    public class CommentHandler : ICommentHandler
    {
        public HandlerResult Handle(IWikifier wikifier, string text)
        {
            return new HandlerResult() { Instructions = [HandlerResultInstruction.TruncateTrailingLine] };
        }
    }
}
