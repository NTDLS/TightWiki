using TightWiki.Plugin.Interfaces.Module;
using TightWiki.Plugin.Interfaces.Module.Handlers;

namespace TightWiki.ViewModels.Admin
{
    public class PluginHandlerViewModel
        : TwViewModel
    {
        public ITwPlugin? Plugin { get; set; }
        public ITwHandlerDescriptor? Handler { get; set; }
    }
}
