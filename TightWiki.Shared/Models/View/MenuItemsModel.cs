using System.Collections.Generic;
using TightWiki.Shared.Models.Data;

namespace TightWiki.Shared.Models.View
{
    public class MenuItemsModel : ModelBase
    {
        public List<MenuItem> Items { get; set; }
    }
}
