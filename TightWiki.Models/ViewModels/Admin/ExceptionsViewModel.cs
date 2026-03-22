using TightWiki.Models.DataModels;

namespace TightWiki.Models.ViewModels.Admin
{
    public class ExceptionsViewModel
        : ViewModelBase
    {
        public List<WikiLogEntry> Exceptions { get; set; } = new();
        public int PaginationPageCount { get; set; }
    }
}
