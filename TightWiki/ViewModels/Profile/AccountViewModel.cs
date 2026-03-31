using TightWiki.Library;
using TightWiki.Plugin.Models;

namespace TightWiki.ViewModels.Profile
{
    public class AccountViewModel
        : TwViewModel
    {
        public List<TwTimeZoneItem> TimeZones { get; set; } = new();
        public List<TwCountryItem> Countries { get; set; } = new();
        public List<TwLanguageItem> Languages { get; set; } = new();
        public List<TwRole> Roles { get; set; } = new();
        public TwAccountProfile Account { get; set; } = new();
    }
}
