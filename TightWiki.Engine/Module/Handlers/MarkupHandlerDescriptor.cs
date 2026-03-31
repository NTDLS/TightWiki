using NTDLS.Helpers;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Module.Handlers;

namespace TightWiki.Engine.Module.Handlers
{
    /// <summary>
    /// Handles basic markup/style instructions like bold, italic, underline, etc.
    /// </summary>
    public class MarkupHandlerDescriptor
        : HandlerDescriptor, ITwMarkupHandler
    {
        public MarkupHandlerDescriptor(ITwHandlerDescriptor descriptor)
            : base(descriptor.EngineModule, descriptor.Method, descriptor.Attribute, descriptor.ModuleAttribute)
        {
        }

        /// <summary>
        /// Handles basic markup instructions like bold, italic, underline, etc.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="sequence">The sequence of symbols that were found to denotate this markup instruction,</param>
        /// <param name="scopeBody">The body of text to apply the style to.</param>
        public async Task<TwPluginResult> Handle(ITwEngineState state, char sequence, string scopeBody)
        {
            var result = (Task<TwPluginResult>)Method.Invoke(EngineModule.Instance, [state, sequence, scopeBody]).EnsureNotNull();
            return await result;
        }
    }
}
