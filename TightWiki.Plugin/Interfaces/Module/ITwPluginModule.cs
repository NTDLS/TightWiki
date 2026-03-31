using TightWiki.Plugin.Attributes;

namespace TightWiki.Plugin.Interfaces.Module
{
    public interface ITwPluginModule
    {
        Type DeclaringType { get; }
        TwPluginModuleAttribute Attribute { get; }
        Interfaces.ITwDisabiguation Instance { get; }
    }
}
