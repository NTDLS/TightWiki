using static TightWiki.Engine.Library.Constants;

namespace TightWiki.Engine.Library
{
    public class HandlerResult
    {
        public string Content { get; set; } = string.Empty;

        public List<HandlerResultInstruction> Instructions { get; set; } = new();

        public HandlerResult()
        {
        }

        public HandlerResult(string content)
        {
            Content = content;
        }
    }
}
