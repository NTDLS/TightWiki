using TightWiki.Models.DataModels;

namespace TightWiki.Models.ViewModels.Admin
{
    public class AccountsViewModel : ViewModelBase
    {
        public List<AccountProfile> Users { get; set; } = new();

        public string SearchToken { get; set; } = string.Empty;
    }
}
