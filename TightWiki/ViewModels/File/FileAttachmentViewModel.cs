using TightWiki.Plugin.Models;

namespace TightWiki.ViewModels.File
{
    public class FileAttachmentViewModel
        : TwViewModel
    {
        public string PageNavigation { get; set; } = string.Empty;
        public int PageRevision { get; set; } = 0;

        public List<TwPageFileAttachmentInfo> Files { get; set; } = new();
    }
}
