using TightWiki.Plugin.Models;

namespace TightWiki.Models.ViewModels.Admin
{
    public class EventLogViewModel
        : ViewModelBase
    {
        public List<TwLogEntry> LogEntries { get; set; } = new();
        public int PaginationPageCount { get; set; }
    }
}
