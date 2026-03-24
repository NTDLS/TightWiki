using TightWiki.Models.DataModels;

namespace TightWiki.Models.ViewModels.Admin
{
    public class EventLogViewModel
        : ViewModelBase
    {
        public List<WikiLogEntry> LogEntries { get; set; } = new();
        public int PaginationPageCount { get; set; }
    }
}
