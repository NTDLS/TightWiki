using TightWiki.Library;
using TightWiki.Models.DataModels;
using TightWiki.Models.ViewModels.Shared;

namespace TightWiki.Models.ViewModels.Admin
{
    public class AccountProfileViewModel : ViewModelBase
    {
        public List<TimeZoneItem> TimeZones { get; set; } = new();
        public List<CountryItem> Countries { get; set; } = new();
        public List<LanguageItem> Languages { get; set; } = new();
        public List<Role> Roles { get; set; } = new();
        public AccountProfileAccountViewModel AccountProfile { get; set; } = new();
        public CredentialViewModel Credential { get; set; } = new();
    }
}
