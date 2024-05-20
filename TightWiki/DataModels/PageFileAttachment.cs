namespace TightWiki.DataModels
{
    public partial class PageFileAttachment
    {
        public int PaginationSize { get; set; }
        public int PaginationCount { get; set; }
        public int Id { get; set; }
        public int PageId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime CreatedDate { get; set; }
        public byte[] Data { get; set; } = new byte[0];
        public string FileNavigation { get; set; } = string.Empty;
        public string PageNavigation { get; set; } = string.Empty;

        public string FriendlySize
        {
            get
            {
                return Wiki.WikiUtility.GetFriendlySize(Size);
            }
        }
    }
}
