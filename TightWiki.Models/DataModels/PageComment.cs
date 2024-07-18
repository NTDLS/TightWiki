namespace TightWiki.Models.DataModels
{
    public partial class PageComment
    {
        public int Id { get; set; }
        public int PageId { get; set; }
        public Guid UserId { get; set; }
        public string Body { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserNavigation { get; set; } = string.Empty;
        public string PageName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public int PaginationPageCount { get; set; }
    }
}
