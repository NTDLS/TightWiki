using NTDLS.Helpers;
using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Interfaces.Module;
using TightWiki.Plugin.Interfaces.Module.Function;
using TightWiki.Plugin.Interfaces.Module.Handlers;

namespace TightWiki.Engine.Module
{
    public class PluginDescriptor(Type declaringType, TwPluginAttribute attribute)
        : ITwPlugin
    {
        public Type DeclaringType { get; private set; } = declaringType;
        public TwPluginAttribute Attribute { get; private set; } = attribute;
        public object Instance { get; private set; } = Activator.CreateInstance(declaringType).EnsureNotNull();

        /// <summary>
        /// List of all functions that are defined in the plugin.
        /// </summary>
        public List<ITwFunctionDescriptor> Functions { get; set; } = new();

        /// <summary>
        /// List of all handlers that are defined in the plugin.
        /// </summary>
        public List<ITwHandlerDescriptor> Handlers { get; set; } = new();
    }
}
