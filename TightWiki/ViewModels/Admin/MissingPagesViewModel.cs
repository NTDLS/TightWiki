using TightWiki.Plugin.Models;

namespace TightWiki.ViewModels.Admin
{
    public class MissingPagesViewModel
        : TwViewModel
    {
        public List<TwNonexistentPage> Pages { get; set; } = new();
        public int PaginationPageCount { get; set; }
    }
}
