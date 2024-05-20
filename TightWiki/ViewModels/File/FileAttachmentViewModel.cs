using TightWiki.DataModels;

namespace TightWiki.ViewModels.File
{
    public class FileAttachmentViewModel : ViewModelBase
    {
        public List<PageFileAttachment> Files { get; set; } = new();
    }
}
