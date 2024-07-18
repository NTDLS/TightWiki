namespace TightWiki.Models.DataModels
{
    public class PageCompilationStatistics
    {
        public string Name { get; set; } = string.Empty;

        public string Title
        {
            get
            {
                if (Name.Contains("::"))
                {
                    return Name.Substring(Name.IndexOf("::") + 2).Trim();
                }
                return Name;
            }
        }

        public string Namespace { get; set; } = string.Empty;
        public string Navigation { get; set; } = string.Empty;
        public DateTime LatestBuild { get; set; }
        public decimal BuildCount { get; set; }
        public decimal AvgBuildTimeMs { get; set; }
        public decimal AvgWikiMatches { get; set; }
        public decimal TotalErrorCount { get; set; }
        public decimal AvgOutgoingLinkCount { get; set; }
        public decimal AvgTagCount { get; set; }
        public decimal AvgRawBodySize { get; set; }
        public decimal AvgWikifiedBodySize { get; set; }

        public int PaginationPageSize { get; set; }
        public int PaginationPageCount { get; set; }
    }
}
