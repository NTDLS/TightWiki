using TightWiki.Models.DataModels;

namespace TightWiki.Models.ViewModels.AdminSecurity
{
    public class RoleViewModel : ViewModelBase
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<AccountProfile> Users { get; set; } = new();
        public int PaginationPageCount { get; set; }
    }
}
