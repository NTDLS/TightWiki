using TightWiki.Plugin.Models;

namespace TightWiki.ViewModels.Admin
{
    public class EventLogViewModel
        : TwViewModel
    {
        public List<TwLogEntry> LogEntries { get; set; } = new();
        public List<TwEventLogSeverity> Severities { get; set; } = new();
        public int PaginationPageCount { get; set; }
        public string? SelectedSeverityName { get; set; }
    }
}
