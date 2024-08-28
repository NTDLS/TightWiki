using TightWiki.Library;

namespace TightWiki.Engine.Library.Interfaces
{
    /// <summary>
    /// Handles links from one wiki page to another.
    /// </summary>
    public interface IInternalLinkHandler
    {
        /// <summary>
        /// Handles an internal wiki link.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="pageNavigation">The navigation for the linked page.</param>
        /// <param name="pageName">The name of the page being linked to.</param>
        /// <param name="linkText">The text which should be show in the absence of an image.</param>
        /// <param name="image">The image that should be shown.</param>
        /// <param name="imageScale">The 0-100 image scale factor for the given image.</param>
        public HandlerResult Handle(ITightEngineState state, NamespaceNavigation pageNavigation, string pageName, string linkText, string? image, int imageScale);
    }
}
