using TightWiki.Plugin.Models;

namespace TightWiki.Models.ViewModels.Admin
{
    public class MissingPagesViewModel
        : ViewModelBase
    {
        public List<TwNonexistentPage> Pages { get; set; } = new();
        public int PaginationPageCount { get; set; }
    }
}
