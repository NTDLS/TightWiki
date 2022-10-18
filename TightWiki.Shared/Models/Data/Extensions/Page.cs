namespace TightWiki.Shared.Models.Data
{
    public partial class Page
    {
        public int TokenWeight { get; set; }
        public string Body { get; set; }
        public string CreatedByUserName { get; set; }
        public string ModifiedByUserName { get; set; }
        public int LatestRevision { get; set; }
        public int PaginationCount { get; set; }
    }
}
