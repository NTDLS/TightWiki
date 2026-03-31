using NTDLS.Helpers;
using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Module;

namespace TightWiki.Engine.Module
{
    public class PluginModule
        : ITwEngineModule
    {
        public Type DeclaringType { get; private set; }
        public TwPluginAttribute Attribute { get; private set; }
        public ITwPlugin Instance { get; private set; }

        public PluginModule(Type declaringType, TwPluginAttribute attribute)
        {
            DeclaringType = declaringType;
            Attribute = attribute;
            Instance = ((ITwPlugin?)Activator.CreateInstance(declaringType)).EnsureNotNull();
        }
    }
}
