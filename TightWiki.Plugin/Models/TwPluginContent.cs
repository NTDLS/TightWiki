namespace TightWiki.Plugin.Models
{
    public class TwPluginContent
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Order { get; set; }
    }
}
