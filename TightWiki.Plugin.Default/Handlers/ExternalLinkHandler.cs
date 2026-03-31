using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Attributes.Handlers;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;

namespace TightWiki.Plugin.Default.Handlers
{
    /// <summary>
    /// Handles links the wiki to another site.
    /// </summary>
    [TwPlugin("Default external link handler", "Handles links the wiki to another site.", 1000)]
    public class ExternalLinkHandler
    {
        /// <summary>
        /// Handles an internal wiki link.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="link">The address of the external site being linked to.</param>
        /// <param name="text">The text which should be show in the absence of an image.</param>
        /// <param name="image">The image that should be shown.</param>
        /// <param name="imageScale">The 0-100 image scale factor for the given image.</param>
        [TwExternalLinkPluginHandler("Default external link handler", "Handles links the wiki to another site.", 1000)]
        public async Task<TwPluginResult> Handle(ITwEngineState state, string link, string? text, string? image)
        {
            if (string.IsNullOrEmpty(image))
            {
                return new TwPluginResult($"<a href=\"{link}\">{text}</a>")
                {
                    Instructions = [HandlerResultInstruction.DisallowNestedProcessing]
                };
            }
            else
            {
                return new TwPluginResult($"<a href=\"{link}\"><img src=\"{image}\" border =\"0\"></a>")
                {
                    Instructions = [HandlerResultInstruction.DisallowNestedProcessing]
                };
            }
        }
    }
}
