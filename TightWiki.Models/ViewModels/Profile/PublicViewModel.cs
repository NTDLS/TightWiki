using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using TightWiki.Models.DataModels;

namespace TightWiki.Models.ViewModels.Profile
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

        [Required(ErrorMessageResourceName = "RequiredAttribute_ValidationError", ErrorMessageResourceType = typeof(Models.Resources.ValTexts))]
        [Display(Name = "Country")]
        public string Country { get; set; } = string.Empty;

        [Required(ErrorMessageResourceName = "RequiredAttribute_ValidationError", ErrorMessageResourceType = typeof(Models.Resources.ValTexts))]
        [Display(Name = "Language")]
        public string Language { get; set; } = string.Empty;

        [Required(ErrorMessageResourceName = "RequiredAttribute_ValidationError", ErrorMessageResourceType = typeof(Models.Resources.ValTexts))]
        [Display(Name = "Time Zone")]
        public string TimeZone { get; set; } = string.Empty;

        public List<PageRevision> RecentlyModified { get; set; } = new();
    }
}
