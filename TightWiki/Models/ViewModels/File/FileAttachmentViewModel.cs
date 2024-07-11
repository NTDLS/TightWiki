using TightWiki.Models.DataModels;

namespace TightWiki.Models.ViewModels.File
{
    public class FileAttachmentViewModel : ViewModelBase
    {
        public string PageNavigation { get; set; } = string.Empty;
        public int PageRevision { get; set; } = 0;

        public List<PageFileAttachmentInfo> Files { get; set; } = new();
    }
}
