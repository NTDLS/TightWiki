using System.Reflection;
using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Attributes.Handlers;
using TightWiki.Plugin.Engine;

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
        /// Regex expression that is used to match the handler call in the markup.
        /// This is used to determine which handler to call when a handler call is encountered in the markup.
        /// </summary>
        public List<TwPluginRegularExpressionAttribute> ExpressionAttributes { get; }

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

        /// <summary>
        /// Processes the specified match string within the given engine state and returns the result asynchronously.
        /// </summary>
        /// <param name="state">The current engine state used to evaluate and process the match. Cannot be null.</param>
        /// <param name="match">The input string to match and process. Cannot be null.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a TwPluginResult describing the
        /// outcome of the processing.</returns>
        Task<TwPluginResult> Handle(ITwEngineState state, string match);
    }
}
