using System.ComponentModel.DataAnnotations;

namespace TightWiki.ViewModels.Shared
{
    public class CredentialViewModel
    {
        public const string NOTSET = "\\__!!_PASSWORD_NOT_SET_!!__//";

        [Required]
        [Display(Name = "Password")]
        [StringLength(50, MinimumLength = 6)]
        public string Password { get; set; } = NOTSET;

        [Required]
        [Display(Name = "Re-enter Password")]
        [StringLength(50, MinimumLength = 6)]
        [Compare("Password")]
        public string ComparePassword { get; set; } = NOTSET;
    }
}
