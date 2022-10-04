using System.ComponentModel.DataAnnotations;

namespace SharpWiki.Shared.Models.View
{
    public class ForgotModel
    {
        [Required]
        [Display(Name = "Email Address")]
        public string EmailAddress { get; set; }
    }
}
