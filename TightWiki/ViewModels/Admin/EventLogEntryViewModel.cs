using TightWiki.Plugin.Models;

namespace TightWiki.ViewModels.Admin
{
    public class EventLogEntryViewModel
        : TwViewModel
    {
        public TwLogEntry LogEntry { get; set; } = new();
    }
}
