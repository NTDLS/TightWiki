using System.Collections.Generic;
using TightWiki.Library.DataModels;

namespace TightWiki.Library.ViewModels.File
{
    public class FileAttachmentViewModel : ViewModelBase
    {
        public List<PageFileAttachment> Files { get; set; } = new();
    }
}
