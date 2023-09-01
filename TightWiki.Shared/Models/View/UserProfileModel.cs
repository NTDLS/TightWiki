using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TightWiki.Shared.Library;
using TightWiki.Shared.Models.Data;

namespace TightWiki.Shared.Models.View
{
    public class UserProfileModel : ModelBase
    {
        public List<TimeZoneItem> TimeZones { get; set; }
        public List<CountryItem> Countries { get; set; }
        public List<LanguageItem> Languages { get; set; }

        public int Id { get; set; }

        [Required]
        [Display(Name = "Email Address")]
        public string EmailAddress { get; set; }

        [Required]
        [Display(Name = "Display Name")]
        public string AccountName { get; set; }

        public string Navigation { get; set; }

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Display(Name = "Personal Bio")]
        public string AboutMe { get; set; }

        [Display(Name = "Avatar")]
        [BindNever]
        public byte[] Avatar { get; set; }

        [Required]
        [Display(Name = "Country")]
        public string Country { get; set; }

        [Required]
        [Display(Name = "Language")]
        public string Language { get; set; }

        [Required]
        [Display(Name = "Time Zone")]
        public string TimeZone { get; set; }
    }
}
