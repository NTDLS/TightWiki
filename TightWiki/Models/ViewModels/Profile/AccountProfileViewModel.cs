using TightWiki.Library;

namespace TightWiki.Models.ViewModels.Profile
{
    public class AccountProfileViewModel : ViewModelBase
    {
        public List<Theme> Themes { get; set; } = new();
        public List<TimeZoneItem> TimeZones { get; set; } = new();
        public List<CountryItem> Countries { get; set; } = new();
        public List<LanguageItem> Languages { get; set; } = new();
        public AccountProfileAccountViewModel AccountProfile { get; set; } = new();
    }
}
