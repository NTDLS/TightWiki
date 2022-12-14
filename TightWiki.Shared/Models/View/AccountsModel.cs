using System.Collections.Generic;
using TightWiki.Shared.Models.Data;

namespace TightWiki.Shared.Models.View
{
    public class AccountsModel : ModelBase
    {
        public List<User> Users { get; set; }

        public string SearchToken { get; set; }
    }
}
