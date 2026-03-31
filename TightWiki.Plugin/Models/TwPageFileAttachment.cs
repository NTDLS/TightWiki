using NTDLS.Helpers;

namespace TightWiki.Plugin.Models
{
    public partial class TwPageFileAttachment
    {
        public int PaginationPageSize { get; set; }
        public int PaginationPageCount { get; set; }
        public int Id { get; set; }
        public int PageId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime CreatedDate { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public string FileNavigation { get; set; } = string.Empty;
        public string PageNavigation { get; set; } = string.Empty;
        public string FriendlySize => Formatters.FileSize(Size);
    }
}
