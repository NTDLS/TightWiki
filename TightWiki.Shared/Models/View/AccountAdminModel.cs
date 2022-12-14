using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Xml.Linq;
using TightWiki.Shared.Library;
using TightWiki.Shared.Models.Data;

namespace TightWiki.Shared.Models.View
{
    public class AccountAdminModel : ModelBase
    {
        public List<TimeZoneItem> TimeZones { get; set; }
        public List<CountryItem> Countries { get; set; }
        public List<LanguageItem> Languages { get; set; }
        public List<Role> Roles { get; set; }
        public User Account { get; set; }

        public Credential Credential { get; set; }
    }

    public class Credential
    {
        public static string NOTSET = "\\__!!_PASSWORD_NOT_SET_!!__//";

        [Required]
        [Display(Name = "Password")]
        [StringLength(50, MinimumLength = 5, ErrorMessage = "Must have a minimum length of 5.")]
        public string Password { get; set; } = NOTSET;

        [Required]
        [Display(Name = "Re-enter Password")]
        [StringLength(50, MinimumLength = 5, ErrorMessage = "Must have a minimum length of 5.")]
        [Compare("Password", ErrorMessage = "The two entered passwords do not match.")]
        public string ComparePassword { get; set; } = NOTSET;

    }
}
