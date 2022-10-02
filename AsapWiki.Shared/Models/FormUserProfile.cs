using System.ComponentModel.DataAnnotations;

namespace AsapWiki.Shared.Models
{
    public class FormUserProfile
    {
        [Required]
        [Display(Name = "Email Address")]
        public string EmailAddress { get; set; }

        [Required]
        [Display(Name = "Display Name")]
        public string AccountName { get; set; }

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

        [Required]
        [Display(Name = "AboutMe")]
        public string AboutMe { get; set; }

        [Required]
        [Display(Name = "Avatar")]
        public byte[] Avatar { get; set; }


    }
}
