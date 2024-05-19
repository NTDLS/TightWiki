using System.Collections.Generic;
using TightWiki.Library.DataModels;

namespace TightWiki.Library.ViewModels.Admin
{
    public class MenuItemsViewModel : ViewModelBase
    {
        public List<MenuItem> Items { get; set; } = new();
    }
}
