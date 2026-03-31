using NTDLS.Helpers;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Engine.Function;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Handlers;

namespace TightWiki.Engine.Handlers
{
    /// <summary>
    /// Handles wiki completion events.
    /// </summary>
    public class TwCompletionHandlerDescriptor
        : TwEngineHandlerDescriptor, ITwCompletionHandler
    {
        public TwCompletionHandlerDescriptor(TwEngineHandlerDescriptor descriptor)
            : base(descriptor.EngineModule, descriptor.Method, descriptor.Attribute, descriptor.ModuleAttribute)
        {
        }

        /// <summary>
        /// Handles wiki completion events. Is called when the wiki processing competes for a given page.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        public async Task<TwHandlerResult> Handle(ITwEngineState state)
        {
            var result = (Task<TwHandlerResult>)Method.Invoke(EngineModule.Instance, [state]).EnsureNotNull();
            return await result;
        }
    }
}


