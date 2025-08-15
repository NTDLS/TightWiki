namespace TightWiki.Models.DataModels
{
    public class PageRevision
    {
        public int PaginationPageSize { get; set; }
        public int PaginationPageCount { get; set; }
        public int PageId { get; set; }
        public string ModifiedByUserName { get; set; } = string.Empty;
        public Guid ModifiedByUserId { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Revision { get; set; }
        public int HigherRevisionCount { get; set; }
        public int HighestRevision { get; set; }
        public string Navigation { get; set; } = string.Empty;
        public Guid CreatedByUserId { get; set; }
        public string CreatedByUserName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string ChangeSummary { get; set; } = string.Empty;
        public string ChangeAnalysis { get; set; } = string.Empty;
    }
}
