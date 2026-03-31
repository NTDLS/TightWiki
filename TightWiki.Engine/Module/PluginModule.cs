using NTDLS.Helpers;
using TightWiki.Plugin.Attributes;

namespace TightWiki.Engine.Module
{
    public class PluginModule
        : Plugin.Interfaces.Module.ITwPluginModule
    {
        public Type DeclaringType { get; private set; }
        public TwPluginModuleAttribute Attribute { get; private set; }
        public Plugin.Interfaces.ITwPluginModule Instance { get; private set; }

        public PluginModule(Type declaringType, TwPluginModuleAttribute attribute)
        {
            DeclaringType = declaringType;
            Attribute = attribute;
            Instance = ((Plugin.Interfaces.ITwPluginModule?)Activator.CreateInstance(declaringType)).EnsureNotNull();
        }
    }
}
