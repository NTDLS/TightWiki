using TightWiki.Library;
using TightWiki.Plugin.Models;

namespace TightWiki.ViewModels.Profile
{
    public class AccountProfileViewModel
        : TwViewModel
    {
        public List<TwTheme> Themes { get; set; } = new();
        public List<TimeZoneItem> TimeZones { get; set; } = new();
        public List<CountryItem> Countries { get; set; } = new();
        public List<LanguageItem> Languages { get; set; } = new();
        public AccountProfileAccountViewModel AccountProfile { get; set; } = new();
    }
}
