using Microsoft.Extensions.Logging;
using NTDLS.Helpers;
using System.Reflection;
using TightWiki.Plugin.Attributes.Functions;
using TightWiki.Plugin.Engine.Function;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Handlers;

namespace TightWiki.Plugin.Engine.Handlers
{
    /// <summary>
    /// Handles exceptions thrown by the wiki engine.
    /// </summary>
    public class TwExceptionHandlerDescriptor
        : TwEngineHandlerDescriptor, ITwExceptionHandler
    {
        public TwExceptionHandlerDescriptor(TwEnginePluginModule engineModule, MethodInfo method, ITwHandlerDescriptorAttribute attribute)
            : base(engineModule, method, attribute)
        {

        }

        /// <summary>
        /// Called when an exception is thrown by the wiki engine.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="customText">Text that accompanies the exception.</param>
        /// <param name="ex">Optional exception, in the case that this was an actual exception.</param>
        public async Task<TwHandlerResult> Handle(ITwEngineState state, LogLevel level, string text, Exception? ex = null)
        {
            var result = (Task<TwHandlerResult>)Method.Invoke(EngineModule.Instance, [state, level, text, ex]).EnsureNotNull();
            return await result;
        }
    }
}
