using System.ComponentModel.DataAnnotations;

namespace SharpWiki.Shared.Models.View
{
    public class UserProfileModel
    {
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

        [Required]
        [Display(Name = "Time Zone")]
        public string TimeZone { get; set; }

        [Required]
        [Display(Name = "Country")]
        public string Country { get; set; }

        [Display(Name = "Personal Bio")]
        public string AboutMe { get; set; }

        [Display(Name = "Avatar")]
        public byte[] Avatar { get; set; }
    }
}
