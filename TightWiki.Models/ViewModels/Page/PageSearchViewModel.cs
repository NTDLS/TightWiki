using TightWiki.Plugin.Models;

namespace TightWiki.Models.ViewModels.Page
{
    public class PageSearchViewModel
        : ViewModelBase
    {
        public List<TwPage> Pages { get; set; } = new();
        public string SearchString { get; set; } = string.Empty;
        public int PaginationPageCount { get; set; }
    }
}
