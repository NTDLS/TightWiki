using System.ComponentModel.DataAnnotations;

namespace TightWiki.Models.ViewModels.Page
{
    public class PageEditViewModel : ViewModelBase
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;
        public string Navigation { get; set; } = string.Empty;
        public string? Description { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
    }
}
