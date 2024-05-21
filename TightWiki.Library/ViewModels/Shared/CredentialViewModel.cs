using System.ComponentModel.DataAnnotations;

namespace TightWiki.ViewModels.Shared
{
    public class CredentialViewModel
    {
        public static string NOTSET = "\\__!!_PASSWORD_NOT_SET_!!__//";

        [Required]
        [Display(Name = "Password")]
        [StringLength(50, MinimumLength = 6, ErrorMessage = "Must have a minimum length of 5.")]
        public string Password { get; set; } = NOTSET;

        [Required]
        [Display(Name = "Re-enter Password")]
        [StringLength(50, MinimumLength = 6, ErrorMessage = "Must have a minimum length of 5.")]
        [Compare("Password", ErrorMessage = "The two entered passwords do not match.")]
        public string ComparePassword { get; set; } = NOTSET;
    }
}
