using TightWiki.Plugin.Models;

namespace TightWiki.Models.ViewModels.Page
{
    public class RevisionsViewModel
        : ViewModelBase
    {
        public List<TwPageRevision> Revisions { get; set; } = new();

        public int PaginationPageCount { get; set; }
    }
}
