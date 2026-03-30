using TightWiki.Plugin.Library;
using TightWiki.Plugin.Models;

namespace TightWiki.ViewModels.Profile
{
    public class AccountProfileViewModel
        : TwViewModel
    {
        public List<TwTheme> Themes { get; set; } = new();
        public List<TwTimeZoneItem> TimeZones { get; set; } = new();
        public List<TwCountryItem> Countries { get; set; } = new();
        public List<TwLanguageItem> Languages { get; set; } = new();
        public AccountProfileAccountViewModel AccountProfile { get; set; } = new();
    }
}
