using TightWiki.Plugin.Models;

namespace TightWiki.Models.ViewModels.Admin
{
    public class EventLogEntryViewModel
        : ViewModelBase
    {
        public TwLogEntry LogEntry { get; set; } = new();
    }
}
