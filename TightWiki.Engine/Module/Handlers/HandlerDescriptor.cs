using System.Reflection;
using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Attributes.Handlers;
using TightWiki.Plugin.Interfaces.Module;
using TightWiki.Plugin.Interfaces.Module.Handlers;

namespace TightWiki.Engine.Module.Handlers
{
    public class HandlerDescriptor
        : ITwHandlerDescriptor
    {
        /// <summary>
        /// Reference to the function that will be called when this function is invoked.
        /// </summary>
        public MethodInfo Method { get; }

        /// <summary>
        /// Attributes of the function, containing information such as the demarcation and description.
        /// This is used to match a function call to its descriptor and for documentation purposes.
        /// </summary>
        public ITwPluginHandlerAttribute HandlerAttribute { get; }

        /// <summary>
        /// The attribute of the module that contains the function, containing information
        /// such as the module name, description, and the order of execution of the module in relation to other modules.
        /// </summary>
        public TwPluginAttribute PluginAttribute { get; }

        /// <summary>
        /// List of parameters that the function accepts, containing information such as the parameter type and name.
        /// Same as method.GetParameters().ToList(), but done here to avoid having to call GetParameters() multiple times, which can be expensive.
        /// </summary>
        public List<ParameterInfo> Parameters { get; }

        /// <summary>
        /// The class that contains the function method.
        /// This is used to invoke the method when the function is called, and can also be used to access any properties
        /// or fields of the class that may be needed for the function's execution.
        /// </summary>
        public ITwPlugin Plugin { get; }

        public HandlerDescriptor(ITwPlugin plugin, MethodInfo method,
            ITwPluginHandlerAttribute handlerAttribute, TwPluginAttribute pluginAttribute)
        {
            Plugin = plugin;
            PluginAttribute = pluginAttribute;
            Method = method;
            HandlerAttribute = handlerAttribute;
            Parameters = method.GetParameters().ToList();
        }
    }
}
