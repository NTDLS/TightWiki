using TightWiki.Models.DataModels;

namespace TightWiki.Models.ViewModels.Admin
{
    public class StatsViewModel : ViewModelBase
    {
        public WikiDatabaseStats DatabaseStats { get; set; } = new();
        public string ApplicationVerson { get; set; } = string.Empty;
    }
}
