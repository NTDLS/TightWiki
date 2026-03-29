using TightWiki.Plugin.Models;

namespace TightWiki.Models.ViewModels.Admin
{
    public class OrphanedPageAttachmentsViewModel
        : ViewModelBase
    {
        public List<TwOrphanedPageAttachment> Files { get; set; } = new();
        public int PaginationPageCount { get; set; }
    }
}
