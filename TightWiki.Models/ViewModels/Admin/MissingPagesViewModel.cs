using TightWiki.Models.DataModels;

namespace TightWiki.Models.ViewModels.Admin
{
    public class MissingPagesViewModel : ViewModelBase
    {
        public List<NonexistentPage> Pages { get; set; } = new();
        public int PaginationPageCount { get; set; }
    }
}
