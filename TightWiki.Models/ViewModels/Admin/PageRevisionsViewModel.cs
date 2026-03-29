using TightWiki.Plugin.Models;

namespace TightWiki.Models.ViewModels.Admin
{
    public class PageRevisionsViewModel
        : ViewModelBase
    {
        public List<TwPageRevision> Revisions { get; set; } = new();

        public int PaginationPageCount { get; set; }
    }
}
