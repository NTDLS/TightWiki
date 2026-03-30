using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;

namespace TightWiki.Engine.Implementation.Handlers
{
    /// <summary>
    /// Handles wiki headings. These are automatically added to the table of contents.
    /// </summary>
    public class HeadingHandler
        : ITwHeadingHandler
    {
        /// <summary>
        /// Handles wiki headings. These are automatically added to the table of contents.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="depth">The size of the header, also used for table of table of contents indentation.</param>
        /// <param name="link">The self link reference.</param>
        /// <param name="text">The text for the self link.</param>
        public async Task<TwHandlerResult> Handle(ITwEngineState state, int depth, string link, string text)
        {
            depth = Math.Clamp(depth, 1, 6);
            string html = $"""<div class="tw-heading tw-heading-{depth}" id="{link}"><a href="#{link}">{text}</a></div>""";
            return new TwHandlerResult(html);
        }
    }
}
