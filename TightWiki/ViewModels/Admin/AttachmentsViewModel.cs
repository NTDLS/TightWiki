using TightWiki.Plugin.Models;

namespace TightWiki.ViewModels.Admin
{
    public class AttachmentsViewModel
        : TwViewModel
    {
        public List<TwPageAttachmentSummary> Files { get; set; } = new();
        public TwPageAttachmentTotals Totals { get; set; } = new();
        public string SearchString { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int PaginationPageCount { get; set; }
    }
}
