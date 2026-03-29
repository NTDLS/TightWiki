namespace TightWiki.Plugin.Models
{
    public class TwPageStatistics
    {
        public string Title
        {
            get
            {
                if (PageName.Contains("::"))
                {
                    return PageName.Substring(PageName.IndexOf("::") + 2).Trim();
                }
                return PageName;
            }
        }

        public string PageName { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public string Navigation { get; set; } = string.Empty;
        public int PageId { get; set; }
        public int Revisions { get; set; }
        public DateTime LastCompileDateTime { get; set; }
        public int TotalCompilationCount { get; set; }
        public decimal LastWikifyTimeMs { get; set; }
        public decimal TotalWikifyTimeMs { get; set; }
        public int TotalViewCount { get; set; }
        public int LastMatchCount { get; set; }
        public int LastErrorCount { get; set; }
        public int LastOutgoingLinkCount { get; set; }
        public int LastTagCount { get; set; }
        public int LastProcessedBodySize { get; set; }
        public int LastBodySize { get; set; }
        public int PaginationPageSize { get; set; }
        public int PaginationPageCount { get; set; }
    }
}
