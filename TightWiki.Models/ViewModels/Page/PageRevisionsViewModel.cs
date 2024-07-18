using TightWiki.Models.DataModels;

namespace TightWiki.Models.ViewModels.Page
{
    public class RevisionsViewModel : ViewModelBase
    {
        public List<PageRevision> Revisions { get; set; } = new();

        public int PaginationPageCount { get; set; }
    }
}
