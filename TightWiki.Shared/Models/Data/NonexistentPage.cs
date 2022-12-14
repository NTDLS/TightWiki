namespace TightWiki.Shared.Models.Data
{
    public class NonexistentPage
    {
        public int SourcePageId { get; set; }
        public string SourcePageName { get; set; }
        public string SourcePageNavigation { get; set; }
        public string TargetPageName { get; set; }
        public string TargetPageNavigation { get; set; }
        public int PaginationCount { get; set; }
    }
}
