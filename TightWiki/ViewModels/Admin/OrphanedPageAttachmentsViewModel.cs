using TightWiki.Plugin.Models;

namespace TightWiki.ViewModels.Admin
{
    public class OrphanedPageAttachmentsViewModel
        : TwViewModel
    {
        public List<TwOrphanedPageAttachment> Files { get; set; } = new();
        public int PaginationPageCount { get; set; }
    }
}
