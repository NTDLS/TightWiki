using TightWiki.Models.DataModels;

namespace TightWiki.Models.ViewModels.Admin
{
    public class SecurityGroupViewModel : ViewModelBase
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        public SecurityGroup ToDataModel()
        {
            return new SecurityGroup
            {
                Name = Name,
                Id = Id,
                Description = Description
            };
        }
    }
}
