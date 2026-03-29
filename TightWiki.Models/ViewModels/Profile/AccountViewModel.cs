using TightWiki.Library;
using TightWiki.Plugin.Models;

namespace TightWiki.Models.ViewModels.Profile
{
    public class AccountViewModel
        : ViewModelBase
    {
        public List<TimeZoneItem> TimeZones { get; set; } = new();
        public List<CountryItem> Countries { get; set; } = new();
        public List<LanguageItem> Languages { get; set; } = new();
        public List<TwRole> Roles { get; set; } = new();
        public TwAccountProfile Account { get; set; } = new();
    }
}
