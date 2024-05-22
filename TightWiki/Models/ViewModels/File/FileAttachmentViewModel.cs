using TightWiki.Models.DataModels;

namespace TightWiki.Models.ViewModels.File
{
    public class FileAttachmentViewModel : ViewModelBase
    {
        public List<PageFileAttachmentInfo> Files { get; set; } = new();
    }
}
