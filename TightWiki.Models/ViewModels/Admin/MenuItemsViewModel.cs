using TightWiki.Models.DataModels;

namespace TightWiki.Models.ViewModels.Admin
{
    public class MenuItemsViewModel : ViewModelBase
    {
        public List<MenuItem> Items { get; set; } = new();
    }
}
