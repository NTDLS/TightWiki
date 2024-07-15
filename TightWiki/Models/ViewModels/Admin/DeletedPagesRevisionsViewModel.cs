using TightWiki.Models.DataModels;

namespace TightWiki.Models.ViewModels.Admin
{
    public class DeletedPagesRevisionsViewModel : ViewModelBase
    {
        public int PageId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public string Navigation { get; set; } = string.Empty;
        public List<DeletedPageRevision> Revisions { get; set; } = new();
        public int PaginationPageCount { get; set; }
    }
}
