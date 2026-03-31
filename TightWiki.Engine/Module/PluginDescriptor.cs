using NTDLS.Helpers;
using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Interfaces.Module;

namespace TightWiki.Engine.Module
{
    public class PluginDescriptor
        : ITwPlugin
    {
        public Type DeclaringType { get; private set; }
        public TwPluginAttribute Attribute { get; private set; }
        public object Instance { get; private set; }

        public PluginDescriptor(Type declaringType, TwPluginAttribute attribute)
        {
            DeclaringType = declaringType;
            Attribute = attribute;
            Instance = Activator.CreateInstance(declaringType).EnsureNotNull();
        }
    }
}
