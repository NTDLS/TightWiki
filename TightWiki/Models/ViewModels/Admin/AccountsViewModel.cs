using TightWiki.Models.DataModels;

namespace TightWiki.Models.ViewModels.Admin
{
    public class AccountsViewModel : ViewModelBase
    {
        public List<AccountProfile> Users { get; set; } = new();

        public string SearchString { get; set; } = string.Empty;
    }
}
