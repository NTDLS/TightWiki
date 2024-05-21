using System.Collections.Generic;
using TightWiki.DataModels;

namespace TightWiki.ViewModels.Admin
{
    public class MenuItemsViewModel : ViewModelBase
    {
        public List<MenuItem> Items { get; set; } = new();
    }
}
