namespace TightWiki.Plugin.Engine
{
    public class TwHandlerResult
    {
        public string Content { get; set; } = string.Empty;

        public List<HandlerResultInstruction> Instructions { get; set; } = new();

        public TwHandlerResult()
        {
        }

        public TwHandlerResult(string content)
        {
            Content = content;
        }
    }
}
