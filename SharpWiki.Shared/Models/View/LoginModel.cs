using System.ComponentModel.DataAnnotations;

namespace SharpWiki.Shared.Models.View
{
    public class LoginModel
	{
        [Required]
        [Display(Name = "Email Address")]
        public string EmailAddress { get; set; }

        [Required]
        [Display(Name = "Password")]
        public string Password { get; set; }
    }
}
