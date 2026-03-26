namespace TightWiki.Models.ViewModels.Admin
{
    public class NamespaceViewModel
        : ViewModelBase
    {
        public List<DataModels.WikiPage> Pages { get; set; } = new();
        public string Namespace { get; set; } = string.Empty;
        public int PaginationPageCount { get; set; }
    }
}
