using System.ComponentModel.DataAnnotations;

namespace TightWiki.Shared.Models.View
{
    public class LoginModel : ModelBase
    {
        [Required]
        [Display(Name = "Email Address")]
        public string EmailAddress { get; set; }

        [Required]
        [Display(Name = "Password")]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }
}
