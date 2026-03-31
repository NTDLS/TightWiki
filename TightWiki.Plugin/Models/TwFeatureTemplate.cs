namespace TightWiki.Plugin.Models
{
    public partial class TwFeatureTemplate
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int PageId { get; set; }
        public string Description { get; set; } = string.Empty;
        public string TemplateText { get; set; } = string.Empty;

        public string HelpPageNavigation { get; set; } = string.Empty;
    }
}
