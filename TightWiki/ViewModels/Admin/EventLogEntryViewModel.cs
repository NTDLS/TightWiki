using TightWiki.Plugin.Models;

namespace TightWiki.ViewModels.Admin
{
    public class EventLogEntryViewModel
        : ViewModelBase
    {
        public TwLogEntry LogEntry { get; set; } = new();
    }
}
