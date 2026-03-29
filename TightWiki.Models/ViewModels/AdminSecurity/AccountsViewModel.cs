using TightWiki.Plugin.Models;

namespace TightWiki.Models.ViewModels.AdminSecurity
{
    public class AccountsViewModel
        : ViewModelBase
    {
        public List<TwAccountProfile> Users { get; set; } = new();
        public string SearchString { get; set; } = string.Empty;
        public int PaginationPageCount { get; set; }
    }
}
