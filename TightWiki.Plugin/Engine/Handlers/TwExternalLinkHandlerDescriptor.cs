using NTDLS.Helpers;
using TightWiki.Plugin.Engine.Function;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Handlers;

namespace TightWiki.Plugin.Engine.Handlers
{
    /// <summary>
    /// Handles links the wiki to another site.
    /// </summary>
    public class TwExternalLinkHandlerDescriptor
        : TwEngineHandlerDescriptor, ITwExternalLinkHandler
    {
        public TwExternalLinkHandlerDescriptor(TwEngineHandlerDescriptor descriptor)
            : base(descriptor.EngineModule, descriptor.Method, descriptor.Attribute, descriptor.ModuleAttribute)
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
        public async Task<TwHandlerResult> Handle(ITwEngineState state, string link, string? text, string? image)
        {
            var result = (Task<TwHandlerResult>)Method.Invoke(EngineModule.Instance, [state, link, text, image]).EnsureNotNull();
            return await result;
        }
    }
}
