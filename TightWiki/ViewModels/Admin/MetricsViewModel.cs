using TightWiki.Plugin.Models;

namespace TightWiki.ViewModels.Admin
{
    public class MetricsViewModel
        : TwViewModel
    {
        public int Exceptions { get; set; }
        public TwWikiDatabaseStatistics Metrics { get; set; } = new();
        public string ApplicationVersion { get; set; } = string.Empty;
    }
}
