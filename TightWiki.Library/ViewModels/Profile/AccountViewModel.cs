using System.Collections.Generic;
using TightWiki.Library.DataModels;
using TightWiki.Library.Library;

namespace TightWiki.Library.ViewModels.Profile
{
    public class AccountViewModel : ViewModelBase
    {
        public List<TimeZoneItem> TimeZones { get; set; } = new();
        public List<CountryItem> Countries { get; set; } = new();
        public List<LanguageItem> Languages { get; set; } = new();
        public List<Role> Roles { get; set; } = new();
        public AccountProfile Account { get; set; } = new();
    }
}
