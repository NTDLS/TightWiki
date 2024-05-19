using System.Collections.Generic;
using TightWiki.Library.Library;
using TightWiki.Library.ViewModels.Shared;

namespace TightWiki.Library.ViewModels.Profile
{
    public class AccountProfileViewModel : ViewModelBase
    {
        public List<TimeZoneItem> TimeZones { get; set; } = new();
        public List<CountryItem> Countries { get; set; } = new();
        public List<LanguageItem> Languages { get; set; } = new();
        public AccountProfileAccountViewModel AccountProfile { get; set; } = new();
        public CredentialViewModel Credential { get; set; } = new();
    }
}
