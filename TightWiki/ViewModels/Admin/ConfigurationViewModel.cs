using TightWiki.Plugin.Library;
using TightWiki.Plugin.Models;

namespace TightWiki.ViewModels.Admin
{
    public class ConfigurationViewModel
        : ViewModelBase
    {
        public List<TwTheme> Themes { get; set; } = new();
        public List<TwRole> Roles { get; set; } = new();
        public List<TwTimeZoneItem> TimeZones { get; set; } = new();
        public List<TwCountryItem> Countries { get; set; } = new();
        public List<TwLanguageItem> Languages { get; set; } = new();
        public List<TwConfigurationNest> Nest { get; set; } = new();
    }
}
