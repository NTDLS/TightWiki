namespace TightWiki.DataModels
{
    public class PageFileRevisionAttachmentInfo
    {
        public int Revision { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public int Size { get; set; }
        public int DataHash { get; set; }
        public int PageId { get; set; }
        public int PageFileId { get; set; }
    }
}
