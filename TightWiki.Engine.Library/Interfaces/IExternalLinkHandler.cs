namespace TightWiki.Engine.Library.Interfaces
{
    /// <summary>
    /// Handles links the wiki to another site.
    /// </summary>
    public interface IExternalLinkHandler
    {
        /// <summary>
        /// Handles an internal wiki link.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="link">The address of the external site being linked to.</param>
        /// <param name="text">The text which should be show in the absence of an image.</param>
        /// <param name="image">The image that should be shown.</param>
        /// <param name="imageScale">The 0-100 image scale factor for the given image.</param>
        public HandlerResult Handle(ITightEngineState state, string link, string? text, string? image);
    }
}
