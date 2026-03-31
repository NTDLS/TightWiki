using NTDLS.Helpers;
using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Interfaces;

namespace TightWiki.Engine.Module
{
    public class PluginModule
        : TightWiki.Plugin.Interfaces.Module.ITwPluginModule
    {
        public Type DeclaringType { get; private set; }
        public TwPluginModuleAttribute Attribute { get; private set; }
        public TightWiki.Plugin.Interfaces.Module.ITwPluginModule Instance { get; private set; }

        public PluginModule(Type declaringType, TwPluginModuleAttribute attribute)
        {
            DeclaringType = declaringType;
            Attribute = attribute;
            Instance = ((TightWiki.Plugin.Interfaces.Module.ITwPluginModule?)Activator.CreateInstance(declaringType)).EnsureNotNull();
        }
    }
}
