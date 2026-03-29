using TightWiki.Library;
using TightWiki.Models.ViewModels.Shared;
using TightWiki.Plugin.Models;

namespace TightWiki.Models.ViewModels.AdminSecurity
{
    public class AccountProfileViewModel
        : ViewModelBase
    {
        public List<TwTheme> Themes { get; set; } = new();
        public List<TimeZoneItem> TimeZones { get; set; } = new();
        public List<CountryItem> Countries { get; set; } = new();
        public List<LanguageItem> Languages { get; set; } = new();
        public List<TwRole> Roles { get; set; } = new();
        public AccountProfileAccountViewModel AccountProfile { get; set; } = new();
        public CredentialViewModel Credential { get; set; } = new();
        public string DefaultRole { get; set; } = string.Empty;
    }
}
