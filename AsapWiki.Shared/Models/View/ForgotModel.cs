using System.ComponentModel.DataAnnotations;

namespace AsapWiki.Shared.Models.View
{
    public class ForgotModel
    {
        [Required]
        [Display(Name = "Email Address")]
        public string EmailAddress { get; set; }
    }
}
