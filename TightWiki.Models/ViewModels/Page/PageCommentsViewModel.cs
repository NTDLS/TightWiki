using TightWiki.Models.DataModels;

namespace TightWiki.Models.ViewModels.Page
{
    public class PageCommentsViewModel : ViewModelBase
    {
        public DataModels.Page Page { get; set; } = new();
        public List<PageComment> Comments { get; set; } = new();
        public string Comment { get; set; } = string.Empty;
        public int PaginationPageCount { get; set; }
    }
}
