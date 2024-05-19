using TightWiki.Library.DataModels;

namespace TightWiki.Library.ViewModels.Admin
{
    public class StatsViewModel : ViewModelBase
    {
        public WikiDatabaseStats DatabaseStats { get; set; } = new();
        public string ApplicationVerson { get; set; } = string.Empty;
    }
}
