using TightWiki.Plugin.Models;

namespace TightWiki.ViewModels.File
{
    public class PageFileRevisionsViewModel
        : TwViewModel
    {
        public string PageNavigation { get; set; } = string.Empty;
        public string FileNavigation { get; set; } = string.Empty;
        public List<TwPageFileAttachmentInfo> Revisions { get; set; } = new();
        public int PaginationPageCount { get; set; }
    }
}
