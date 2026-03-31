using Microsoft.Extensions.Logging;
using NTDLS.Helpers;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Module.Handlers;

namespace TightWiki.Engine.Module.Handlers
{
    /// <summary>
    /// Handles exceptions thrown by the wiki engine.
    /// </summary>
    public class ExceptionHandlerDescriptor
        : HandlerDescriptor, ITwExceptionPlugin
    {
        public ExceptionHandlerDescriptor(ITwHandlerDescriptor descriptor)
            : base(descriptor.Plugin, descriptor.Method, descriptor.Attribute, descriptor.ModuleAttribute)
        {
        }

        /// <summary>
        /// Called when an exception is thrown by the wiki engine.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="customText">Text that accompanies the exception.</param>
        /// <param name="ex">Optional exception, in the case that this was an actual exception.</param>
        public async Task<TwPluginResult> Handle(ITwEngineState state, LogLevel level, string text, Exception? ex = null)
        {
            var result = (Task<TwPluginResult>)Method.Invoke(Plugin.Instance, [state, level, text, ex]).EnsureNotNull();
            return await result;
        }
    }
}
