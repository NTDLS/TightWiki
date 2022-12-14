using System.ComponentModel.DataAnnotations;

namespace TightWiki.Shared.Models.View
{
    public class EditPageModel : ModelBase
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string Navigation { get; set; }
        public string Description { get; set; }
        public string Body { get; set; }
    }
}
