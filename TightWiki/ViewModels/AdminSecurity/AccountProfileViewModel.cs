using TightWiki.Library;
using TightWiki.Plugin.Models;
using TightWiki.ViewModels.Shared;

namespace TightWiki.ViewModels.AdminSecurity
{
    public class AccountProfileViewModel
        : TwViewModel
    {
        public List<TwTheme> Themes { get; set; } = new();
        public List<TwTimeZoneItem> TimeZones { get; set; } = new();
        public List<TwCountryItem> Countries { get; set; } = new();
        public List<TwLanguageItem> Languages { get; set; } = new();
        public List<TwRole> Roles { get; set; } = new();
        public AccountProfileAccountViewModel AccountProfile { get; set; } = new();
        public CredentialViewModel Credential { get; set; } = new();
        public string DefaultRole { get; set; } = string.Empty;
    }
}
