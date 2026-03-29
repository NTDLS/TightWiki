using TightWiki.Plugin.Models;

namespace TightWiki.Models.ViewModels.Page
{
    public class PageCommentsViewModel
        : ViewModelBase
    {
        public List<TwPageComment> Comments { get; set; } = new();
        public string Comment { get; set; } = string.Empty;
        public int PaginationPageCount { get; set; }
    }
}
