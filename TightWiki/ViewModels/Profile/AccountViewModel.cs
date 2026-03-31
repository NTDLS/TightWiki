using TightWiki.Library;
using TightWiki.Plugin.Models;

namespace TightWiki.ViewModels.Profile
{
    public class AccountViewModel
        : TwViewModel
    {
        public List<TimeZoneItem> TimeZones { get; set; } = new();
        public List<CountryItem> Countries { get; set; } = new();
        public List<LanguageItem> Languages { get; set; } = new();
        public List<TwRole> Roles { get; set; } = new();
        public TwAccountProfile Account { get; set; } = new();
    }
}
