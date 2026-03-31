using TightWiki.Plugin.Models;

namespace TightWiki.ViewModels.Admin
{
    public class DatabaseViewModel
        : TwViewModel
    {
        public List<TwDatabaseInfo> Info { get; set; } = new();
    }
}
