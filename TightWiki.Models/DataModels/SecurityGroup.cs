using TightWiki.Models.ViewModels.Admin;

namespace TightWiki.Models.DataModels
{
    public partial class SecurityGroup
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        public SecurityGroupViewModel ToViewModel()
        {
            return new SecurityGroupViewModel
            {
                Name = Name,
                Id = Id,
                Description = Description
            };
        }
    }
}
