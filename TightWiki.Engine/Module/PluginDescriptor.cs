using NTDLS.Helpers;
using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Interfaces.Module;

namespace TightWiki.Engine.Module
{
    public class PluginDescriptor(Type declaringType, TwPluginAttribute attribute)
        : ITwPlugin
    {
        public Type DeclaringType { get; private set; } = declaringType;
        public TwPluginAttribute Attribute { get; private set; } = attribute;
        public object Instance { get; private set; } = Activator.CreateInstance(declaringType).EnsureNotNull();
    }
}
