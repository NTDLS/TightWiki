using TightWiki.Library;
using TightWiki.Models.DataModels;

namespace TightWiki.Models.ViewModels.Admin
{
    public class ConfigurationViewModel : ViewModelBase
    {
        public List<Theme> Themes { get; set; } = new();
        public List<Role> Roles { get; set; } = new();
        public List<TimeZoneItem> TimeZones { get; set; } = new();
        public List<CountryItem> Countries { get; set; } = new();
        public List<LanguageItem> Languages { get; set; } = new();
        public List<ConfigurationNest> Nest { get; set; } = new();
    }
}
