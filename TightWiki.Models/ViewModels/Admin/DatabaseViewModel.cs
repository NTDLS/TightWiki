using TightWiki.Models.DataModels;

namespace TightWiki.Models.ViewModels.Admin
{
    public class DatabaseViewModel : ViewModelBase
    {
        public List<DatabaseInfo> Info { get; set; } = new();
    }
}
