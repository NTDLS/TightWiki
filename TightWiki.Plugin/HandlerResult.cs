namespace TightWiki.Plugin
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
