namespace TightWiki.Plugin.Models
{
    public class TwPluginContent
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Type? Type { get; set; }
        public int Precedence { get; set; }
    }
}
