using TightWiki.Plugin.Models;

namespace TightWiki.ViewModels.Admin
{
    public class PageRevisionsViewModel
        : TwViewModel
    {
        public List<TwPageRevision> Revisions { get; set; } = new();

        public int PaginationPageCount { get; set; }
    }
}
