using TightWiki.Plugin;
using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Attributes.Functions;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Handlers;

namespace TightWiki.Engine.Implementation.Handlers
{
    /// <summary>
    /// Handles links the wiki to another site.
    /// </summary>
    [TwPluginModule("Default external link handler", "Handles links the wiki to another site.")]
    public class ExternalLinkHandler
        : ITwExternalLinkHandler
    {
        /// <summary>
        /// Handles an internal wiki link.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="link">The address of the external site being linked to.</param>
        /// <param name="text">The text which should be show in the absence of an image.</param>
        /// <param name="image">The image that should be shown.</param>
        /// <param name="imageScale">The 0-100 image scale factor for the given image.</param>
        [TwExternalLinkHandler("Default external link handler", "Handles links the wiki to another site.")]
        public async Task<TwHandlerResult> Handle(ITwEngineState state, string link, string? text, string? image)
        {
            if (string.IsNullOrEmpty(image))
            {
                return new TwHandlerResult($"<a href=\"{link}\">{text}</a>")
                {
                    Instructions = [HandlerResultInstruction.DisallowNestedProcessing]
                };
            }
            else
            {
                return new TwHandlerResult($"<a href=\"{link}\"><img src=\"{image}\" border =\"0\"></a>")
                {
                    Instructions = [HandlerResultInstruction.DisallowNestedProcessing]
                };
            }
        }
    }
}
