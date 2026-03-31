using System.Reflection;
using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Attributes.Handlers;

namespace TightWiki.Plugin.Interfaces.Module.Handlers
{
    /// <summary>
    /// Base interface for a handler descriptor, which contains information about a handler that can be called by the engine.
    /// </summary>
    public interface ITwHandlerDescriptor
    {
        /// <summary>
        /// Reference to the handler that will be called when this handler is invoked.
        /// </summary>
        MethodInfo Method { get; }

        /// <summary>
        /// Attributes of the handler, containing information such as the demarcation and description.
        /// This is used to match a handler call to its descriptor and for documentation purposes.
        /// </summary>
        ITwPluginHandlerAttribute HandlerAttribute { get; }

        /// <summary>
        /// The attribute of the module that contains the handler, containing information
        /// such as the module name, description, and the order of execution of the module in relation to other modules.
        /// </summary>
        TwPluginAttribute PluginAttribute { get; }

        /// <summary>
        /// List of parameters that the handler accepts, containing information such as the parameter type and name.
        /// Same as method.GetParameters().ToList(), but done here to avoid having to call GetParameters() multiple times, which can be expensive.
        /// </summary>
        List<ParameterInfo> Parameters { get; }

        /// <summary>
        /// The class that contains the handler method.
        /// This is used to invoke the method when the function is called, and can also be used to access any properties
        /// or fields of the class that may be needed for the function's execution.
        /// </summary>
        ITwPlugin Plugin { get; }
    }
}
