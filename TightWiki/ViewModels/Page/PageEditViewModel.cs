using System.ComponentModel.DataAnnotations;
using TightWiki.Plugin.Models;

namespace TightWiki.ViewModels.Page
{
    public class PageEditViewModel
        : TwViewModel
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;
        public string Navigation { get; set; } = string.Empty;
        public string? Description { get; set; } = string.Empty;
        public string? ChangeSummary { get; set; } = string.Empty;
        public string? Body { get; set; } = string.Empty;
        public List<TwPage> Templates { get; set; } = new();
        public List<TwFeatureTemplate> FeatureTemplates { get; set; } = new();
    }
}
