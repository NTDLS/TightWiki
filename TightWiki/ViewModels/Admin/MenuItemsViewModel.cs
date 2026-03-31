using TightWiki.Plugin.Models;

namespace TightWiki.ViewModels.Admin
{
    public class MenuItemsViewModel
        : TwViewModel
    {
        public List<TwMenuItem> Items { get; set; } = new();
    }
}
