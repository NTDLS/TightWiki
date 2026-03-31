using TightWiki.Plugin.Models;

namespace TightWiki.ViewModels.Page
{
    public class PageCommentsViewModel
        : TwViewModel
    {
        public List<TwPageComment> Comments { get; set; } = new();
        public string Comment { get; set; } = string.Empty;
        public int PaginationPageCount { get; set; }
    }
}
