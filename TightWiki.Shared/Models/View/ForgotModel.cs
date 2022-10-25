using System.ComponentModel.DataAnnotations;

namespace TightWiki.Shared.Models.View
{
    public class ForgotModel : ModelBase
    {
        [Required]
        [Display(Name = "Email Address")]
        public string EmailAddress { get; set; }
    }
}
