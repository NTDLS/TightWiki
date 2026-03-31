using NTDLS.Helpers;
using TightWiki.Plugin;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Module.Handlers;

namespace TightWiki.Engine.Module.Handlers
{
    /// <summary>
    /// Handles links from one wiki page to another.
    /// </summary>
    public class InternalLinkHandlerDescriptor
        : HandlerDescriptor, ITwInternalLinkPlugin
    {
        public InternalLinkHandlerDescriptor(ITwHandlerDescriptor descriptor)
            : base(descriptor.EngineModule, descriptor.Method, descriptor.Attribute, descriptor.ModuleAttribute)
        {
        }

        /// <summary>
        /// Handles an internal wiki link.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="pageNavigation">The navigation for the linked page.</param>
        /// <param name="pageName">The name of the page being linked to.</param>
        /// <param name="linkText">The text which should be show in the absence of an image.</param>
        /// <param name="image">The image that should be shown.</param>
        /// <param name="imageScale">The 0-100 image scale factor for the given image.</param>
        public async Task<TwPluginResult> Handle(ITwEngineState state, TwNamespaceNavigation pageNavigation, string pageName, string linkText, string? image, int imageScale)
        {
            var result = (Task<TwPluginResult>)Method.Invoke(EngineModule.Instance, [state, pageNavigation, pageName, linkText, image, imageScale]).EnsureNotNull();
            return await result;
        }
    }
}
