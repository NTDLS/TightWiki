using NTDLS.Helpers;
using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Module;

namespace TightWiki.Engine.Module
{
    public class TwEnginePluginModule
        : ITwEnginePluginModule
    {
        public Type DeclaringType { get; private set; }
        public TwPluginModuleAttribute Attribute { get; private set; }
        public ITwPluginModule Instance { get; private set; }

        public TwEnginePluginModule(Type declaringType, TwPluginModuleAttribute attribute)
        {
            DeclaringType = declaringType;
            Attribute = attribute;
            Instance = ((ITwPluginModule?)Activator.CreateInstance(declaringType)).EnsureNotNull();
        }
    }
}
