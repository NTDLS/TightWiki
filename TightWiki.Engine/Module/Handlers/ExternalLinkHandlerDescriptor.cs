using NTDLS.Helpers;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Module.Handlers;

namespace TightWiki.Engine.Module.Handlers
{
    /// <summary>
    /// Handles links the wiki to another site.
    /// </summary>
    public class ExternalLinkHandlerDescriptor
        : HandlerDescriptor, ITwExternalLinkPlugin
    {
        public ExternalLinkHandlerDescriptor(ITwHandlerDescriptor descriptor)
            : base(descriptor.Plugin, descriptor.Method, descriptor.Attribute, descriptor.ModuleAttribute)
        {
        }

        /// <summary>
        /// Handles an internal wiki link.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="link">The address of the external site being linked to.</param>
        /// <param name="text">The text which should be show in the absence of an image.</param>
        /// <param name="image">The image that should be shown.</param>
        /// <param name="imageScale">The 0-100 image scale factor for the given image.</param>
        public async Task<TwPluginResult> Handle(ITwEngineState state, string link, string? text, string? image)
        {
            var result = (Task<TwPluginResult>)Method.Invoke(Plugin.Instance, [state, link, text, image]).EnsureNotNull();
            return await result;
        }
    }
}
