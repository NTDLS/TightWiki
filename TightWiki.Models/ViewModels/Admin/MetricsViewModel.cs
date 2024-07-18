using TightWiki.Models.DataModels;

namespace TightWiki.Models.ViewModels.Admin
{
    public class MetricsViewModel : ViewModelBase
    {
        public WikiDatabaseStatistics Metrics { get; set; } = new();
        public string ApplicationVersion { get; set; } = string.Empty;
    }
}
