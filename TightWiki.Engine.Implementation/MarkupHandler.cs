using TightWiki.Engine.Library;
using TightWiki.Engine.Library.Interfaces;
using static TightWiki.Engine.Library.Constants;

namespace TightWiki.Engine.Implementation
{
    public class MarkupHandler : IMarkupHandler
    {
        public HandlerResult Handle(ITightEngineState state, char sequence, string scopeBody)
        {
            switch (sequence)
            {
                case '~': return new HandlerResult($"<strike>{scopeBody}</strike>");
                case '*': return new HandlerResult($"<strong>{scopeBody}</strong>");
                case '_': return new HandlerResult($"<u>{scopeBody}</u>");
                case '/': return new HandlerResult($"<i>{scopeBody}</i>");
                case '!': return new HandlerResult($"<mark>{scopeBody}</mark>");
            }

            return new HandlerResult() { Instructions = [HandlerResultInstruction.Skip] };
        }
    }
}
