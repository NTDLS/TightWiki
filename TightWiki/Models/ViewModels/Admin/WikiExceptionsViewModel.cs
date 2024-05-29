using TightWiki.Models.DataModels;

namespace TightWiki.Models.ViewModels.Admin
{
    public class WikiExceptionsViewModel : ViewModelBase
    {
        public List<WikiException> Exceptions { get; set; } = new();
        public int PaginationPageCount { get; set; }
    }
}
