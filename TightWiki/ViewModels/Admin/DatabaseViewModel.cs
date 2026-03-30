using TightWiki.Plugin.Models;

namespace TightWiki.ViewModels.Admin
{
    public class DatabaseViewModel
        : ViewModelBase
    {
        public List<TwDatabaseInfo> Info { get; set; } = new();
    }
}
