using TightWiki.Plugin.Attributes;

namespace TightWiki.Plugin.Interfaces.Module
{
    public interface ITwEnginePluginModule
    {
        Type DeclaringType { get; }
        TwPluginModuleAttribute Attribute { get; }
        ITwPluginModule Instance { get; }
    }
}
