using System;

namespace TightWiki.DataModels
{
    public class PageRevisionHistory
    {
        public int PaginationSize { get; set; }
        public int PaginationCount { get; set; }
        public int PageId { get; set; }
        public string ModifiedByUserName { get; set; } = string.Empty;
        public Guid ModifiedByUserId { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Revision { get; set; }
        public string Navigation { get; set; } = string.Empty;
        public Guid CreatedByUserId { get; set; }
        public string CreatedByUserName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string ChangeSummary { get; set; } = string.Empty;
    }
}
