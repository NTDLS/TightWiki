using TightWiki.Models.DataModels;

namespace TightWiki.Models.ViewModels.File
{
    public class FileAttachmentViewModel : ViewModelBase
    {
        public List<PageFileAttachment> Files { get; set; } = new();
    }
}
