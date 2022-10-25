using System.ComponentModel.DataAnnotations;

namespace TightWiki.Shared.Models.View
{
    public class SignupModel : ModelBase
    {
        [Required]
        [Display(Name = "Email Address")]
        public string EmailAddress { get; set; }

        [Required]
        [Display(Name = "Display Name")]
        public string AccountName { get; set; }

        [Required]
        [Display(Name = "Password")]
        [StringLength(50, MinimumLength = 5, ErrorMessage = "Must have a minimum length of 5.")]
        public string Password { get; set; }

        [Required]
        [Display(Name = "Re-enter Password")]
        [StringLength(50, MinimumLength = 5, ErrorMessage = "Must have a minimum length of 5.")]
        [Compare("Password", ErrorMessage = "The two entered passwords do not match.")]
        public string ComparePassword { get; set; }

        public string Navigation { get; set; }

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required]
        [Display(Name = "Time Zone")]
        public string TimeZone { get; set; }

        [Required]
        [Display(Name = "Country")]
        public string Country { get; set; }
    }
}
