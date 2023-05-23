using System;

namespace TightWiki.Shared.Models.Data
{
    public partial class PageComment
    {
        public int Id { get; set; }
        public int PageId { get; set; }
        public int UserId { get; set; }
        public string Body { get; set; }
        public string UserName { get; set; }
        public string UserNavigation { get; set; }
        public string PageName { get; set; }
        public DateTime CreatedDate { get; set; }
        public int PaginationCount { get; set; }
    }
}
