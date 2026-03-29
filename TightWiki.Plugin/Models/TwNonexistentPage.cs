namespace TightWiki.Plugin.Models
{
    public class TwNonexistentPage
    {
        public int SourcePageId { get; set; }
        public string SourcePageName { get; set; } = string.Empty;
        public string SourcePageNavigation { get; set; } = string.Empty;
        public string TargetPageName { get; set; } = string.Empty;
        public string TargetPageNavigation { get; set; } = string.Empty;
        public int PaginationPageCount { get; set; }
    }
}
