using System.ComponentModel.DataAnnotations;

namespace TightWiki.Models.ViewModels.Shared
{
    public class CredentialViewModel
    {
        public const string NOTSET = "\\__!!_PASSWORD_NOT_SET_!!__//";

        [Required(ErrorMessageResourceName = "RequiredAttribute_ValidationError", ErrorMessageResourceType = typeof(Models.Resources.ValTexts))]
        [Display(Name = "Password")]
        [StringLength(50, MinimumLength = 6, ErrorMessageResourceName = "StringLengthAttribute_ValidationErrorIncludingMinimum", ErrorMessageResourceType = typeof(Models.Resources.ValTexts))]
        public string Password { get; set; } = NOTSET;

        [Required(ErrorMessageResourceName = "RequiredAttribute_ValidationError", ErrorMessageResourceType = typeof(Models.Resources.ValTexts))]
        [Display(Name = "Re-enter Password")]
        [StringLength(50, MinimumLength = 6, ErrorMessageResourceName = "StringLengthAttribute_ValidationErrorIncludingMinimum", ErrorMessageResourceType = typeof(Models.Resources.ValTexts))]
        [Compare("Password", ErrorMessageResourceName = "CompareAttribute_MustMatch", ErrorMessageResourceType = typeof(Models.Resources.ValTexts))]
        public string ComparePassword { get; set; } = NOTSET;
    }
}
