namespace TightWiki.Plugin.Models.Defaults
{
    public class TwDefaultWikiPage
    {
        public string Name { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public string Navigation { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Revision { get; set; }
        public int DataHash { get; set; }
        public string Body { get; set; } = string.Empty;
    }
}
