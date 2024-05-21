using TightWiki.DataModels;

namespace TightWiki.ViewModels.Admin
{
    public class StatsViewModel : ViewModelBase
    {
        public WikiDatabaseStats DatabaseStats { get; set; } = new();
        public string ApplicationVerson { get; set; } = string.Empty;
    }
}
