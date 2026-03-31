using TightWiki.Plugin.Models;

namespace TightWiki.ViewModels.Admin
{
    public class PluginsViewModel
        : TwViewModel
    {
        public List<TwPlugin> Plugins { get; set; } = new();
    }
}
