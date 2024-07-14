namespace TightWiki.Models.DataModels
{
    public partial class OrphanedPageAttachment
    {
        public int PaginationPageCount { get; set; }
        public int PageFileId { get; set; }
        public string PageName { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public string PageNavigation { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FileNavigation { get; set; } = string.Empty; //https://localhost:7053/File/Binary/Home/test_file_b_txt/1
        public long Size { get; set; }
        public int FileRevision { get; set; }

        public string PageTitle
        {
            get
            {
                if (PageName.Contains("::"))
                {
                    return PageName.Substring(PageName.IndexOf("::") + 2).Trim();
                }
                return PageName;
            }
        }
    }
}
