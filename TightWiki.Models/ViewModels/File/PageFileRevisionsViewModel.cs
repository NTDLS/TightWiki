using TightWiki.Models.DataModels;

namespace TightWiki.Models.ViewModels.File
{
    public class PageFileRevisionsViewModel : ViewModelBase
    {
        public string PageNavigation { get; set; } = string.Empty;
        public string FileNavigation { get; set; } = string.Empty;
        public List<PageFileAttachmentInfo> Revisions { get; set; } = new();
        public int PaginationPageCount { get; set; }
    }
}
