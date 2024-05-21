using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TightWiki.DataModels;

namespace TightWiki.ViewModels.Profile
{
    public class PublicViewModel : ViewModelBase
    {
        public string Navigation { get; set; } = string.Empty;
        public Guid Id { get; set; }

        [Display(Name = "Name")]
        public string AccountName { get; set; } = string.Empty;

        [Display(Name = "Personal Bio")]
        public string Biography { get; set; } = string.Empty;

        [Display(Name = "Avatar")]
        [BindNever]
        public byte[]? Avatar { get; set; }

        [Required]
        [Display(Name = "Country")]
        public string Country { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Language")]
        public string Language { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Time Zone")]
        public string TimeZone { get; set; } = string.Empty;

        public List<PageRevisionHistory> RecentlyModified { get; set; } = new();
    }
}
