using TightWiki.Plugin.Models;

namespace TightWiki.ViewModels.Page
{
    public class PageSearchViewModel
        : TwViewModel
    {
        public List<TwPage> Pages { get; set; } = new();
        public string SearchString { get; set; } = string.Empty;
        public int PaginationPageCount { get; set; }
    }
}
