namespace TightWiki.Models.ViewModels.Admin
{
    public class PagesViewModel
        : ViewModelBase
    {
        public List<DataModels.WikiPage> Pages { get; set; } = new();
        public string SearchString { get; set; } = string.Empty;
        public int PaginationPageCount { get; set; }
    }
}
