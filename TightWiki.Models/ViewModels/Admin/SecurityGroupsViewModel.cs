using TightWiki.Models.DataModels;

namespace TightWiki.Models.ViewModels.Admin
{
    public class SecurityGroupsViewModel : ViewModelBase
    {
        public List<SecurityGroup> Groups { get; set; } = new();
    }
}
