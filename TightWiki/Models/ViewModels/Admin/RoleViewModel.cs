using TightWiki.Models.DataModels;

namespace TightWiki.Models.ViewModels.Admin
{
    public class RoleViewModel : ViewModelBase
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<AccountProfile> Users { get; set; } = new();
    }
}
