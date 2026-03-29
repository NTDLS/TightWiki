using TightWiki.Plugin.Models;

namespace TightWiki.Models.ViewModels.Admin
{
    public class MenuItemsViewModel
        : ViewModelBase
    {
        public List<TwMenuItem> Items { get; set; } = new();
    }
}
