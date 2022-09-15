using System.ComponentModel.DataAnnotations;

namespace AsapWiki.Shared.Models
{
    public class FormReset
    {
        [Required]
        [Display(Name = "Email Address")]
        public string EmailAddress { get; set; }

        [Required]
        [Display(Name = "Temp Code")]
        public string TempCode { get; set; }

        [Required]
        [Display(Name = "Password")]
        [StringLength(50, MinimumLength = 5, ErrorMessage = "Must have a minimum length of 5.")]
        public string Password { get; set; }

        [Required]
        [Display(Name = "Re-enter Password")]
        [StringLength(50, MinimumLength = 5, ErrorMessage = "Must have a minimum length of 5.")]
        [Compare("Password", ErrorMessage = "The two entered passwords do not match.")]
        public string ComparePassword { get; set; }
    }
}
