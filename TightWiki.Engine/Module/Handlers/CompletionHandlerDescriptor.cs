using NTDLS.Helpers;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Module.Handlers;

namespace TightWiki.Engine.Module.Handlers
{
    /// <summary>
    /// Handles wiki completion events.
    /// </summary>
    public class CompletionHandlerDescriptor(ITwHandlerDescriptor descriptor)
        : HandlerDescriptor(descriptor.Plugin, descriptor.Method, descriptor.HandlerAttribute, descriptor.PluginAttribute), ITwCompletionPlugin
    {

        /// <summary>
        /// Handles wiki completion events. Is called when the wiki processing competes for a given page.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        public async Task<TwPluginResult> Handle(ITwEngineState state)
        {
            var result = (Task<TwPluginResult>)Method.Invoke(Plugin.Instance, [state]).EnsureNotNull();
            return await result;
        }
    }
}


