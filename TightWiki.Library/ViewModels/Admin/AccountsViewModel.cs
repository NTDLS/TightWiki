using System.Collections.Generic;
using TightWiki.DataModels;

namespace TightWiki.ViewModels.Admin
{
    public class AccountsViewModel : ViewModelBase
    {
        public List<AccountProfile> Users { get; set; } = new();

        public string SearchToken { get; set; } = string.Empty;
    }
}
