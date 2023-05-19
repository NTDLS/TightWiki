using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TightWiki.Shared.Models.Data;

namespace TightWiki.Shared.Models.View
{
    public class PublicProfileModel : ModelBase
    {
        public string Navigation { get; set; }
        public int Id { get; set; }

        [Display(Name = "Name")]
        public string AccountName { get; set; }

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

        public List<Page> RecentlyModified { get; set; }
    }
}
