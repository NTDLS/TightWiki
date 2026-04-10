using TightWiki.Plugin.Interfaces.Module;
using TightWiki.Plugin.Interfaces.Module.Function;

namespace TightWiki.ViewModels.Admin
{
    public class PluginFunctionViewModel
        : TwViewModel
    {
        public ITwPlugin? Plugin { get; set; }
        public ITwFunctionDescriptor? Function { get; set; }
    }
}
