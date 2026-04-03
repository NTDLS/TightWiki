using TightWiki.Plugin.Models;

namespace TightWiki.ViewModels.Admin
{
    public class PluginClassViewModel
        : TwViewModel
    {
        public TwPlugin? Plugin { get; set; }
        public List<TwPluginContent> Contents { get; set; } = new();
    }
}
