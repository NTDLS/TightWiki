using TightWiki.Models.DataModels;

namespace TightWiki.Models.ViewModels.Admin
{
    public class EventLogEntryViewModel
        : ViewModelBase
    {
        public WikiLogEntry LogEntry { get; set; } = new();
    }
}
