using TightWiki.Models.DataModels;

namespace TightWiki.Models.ViewModels.Admin
{
    public class StatisticsViewModel : ViewModelBase
    {
        public WikiDatabaseStatistics Statistics { get; set; } = new();
        public string ApplicationVersion { get; set; } = string.Empty;
    }
}
