using TightWiki.Library;
using TightWiki.Plugin.Models;

namespace TightWiki.ViewModels.Admin
{
    public class ConfigurationViewModel
        : TwViewModel
    {
        public List<TwTheme> Themes { get; set; } = new();
        public List<TwRole> Roles { get; set; } = new();
        public List<TimeZoneItem> TimeZones { get; set; } = new();
        public List<CountryItem> Countries { get; set; } = new();
        public List<LanguageItem> Languages { get; set; } = new();
        public List<TwConfigurationNest> Nest { get; set; } = new();
    }
}
