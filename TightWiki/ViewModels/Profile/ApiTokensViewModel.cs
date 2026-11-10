using System.ComponentModel.DataAnnotations;
using TightWiki.Repository.Models;

namespace TightWiki.ViewModels.Profile
{
    public class ApiTokensViewModel
        : TwViewModel
    {
        public List<TwApiToken> Tokens { get; set; } = new();

        [Display(Name = "Token Name")]
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The plain text of a token that was just created. It is shown once and is never stored.
        /// </summary>
        public string? NewToken { get; set; }
    }
}
