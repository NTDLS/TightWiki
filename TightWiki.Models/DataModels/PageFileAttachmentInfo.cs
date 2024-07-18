namespace TightWiki.Models.DataModels
{
    public partial class PageFileAttachmentInfo
    {
        public int PaginationPageSize { get; set; }
        public int PaginationPageCount { get; set; }
        public int Id { get; set; }
        public int PageId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime CreatedDate { get; set; }
        public string FileNavigation { get; set; } = string.Empty;
        public string PageNavigation { get; set; } = string.Empty;
        public int FileRevision { get; set; }
        public Guid CreatedByUserId { get; set; }
        public string CreatedByUserName { get; set; } = string.Empty;
        public string CreatedByNavigation { get; set; } = string.Empty;
        public string FriendlySize => NTDLS.Helpers.Formatters.FileSize(Size);
    }
}
