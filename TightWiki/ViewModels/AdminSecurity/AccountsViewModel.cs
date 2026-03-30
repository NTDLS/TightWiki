using TightWiki.Plugin.Models;

namespace TightWiki.ViewModels.AdminSecurity
{
    public class AccountsViewModel
        : TwViewModel
    {
        public List<TwAccountProfile> Users { get; set; } = new();
        public string SearchString { get; set; } = string.Empty;
        public int PaginationPageCount { get; set; }
    }
}
