using System.ComponentModel.DataAnnotations;

namespace AsapWiki.Shared.Models
{
    public class FormForgot
    {
        [Required]
        [Display(Name = "Email Address")]
        public string EmailAddress { get; set; }
    }
}
