using TightWiki.Engine.Library;
using TightWiki.Engine.Library.Interfaces;

namespace TightWiki.Engine.Implementation.Handlers
{
    /// <summary>
    /// Handles wiki headings. These are automatically added to the table of contents.
    /// </summary>
    public class HeadingHandler : IHeadingHandler
    {
        /// <summary>
        /// Handles wiki headings. These are automatically added to the table of contents.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="depth">The size of the header, also used for table of table of contents indentation.</param>
        /// <param name="link">The self link reference.</param>
        /// <param name="text">The text for the self link.</param>
        public HandlerResult Handle(ITightEngineState state, int depth, string link, string text)
        {
            double fontSize = 2.2 - (depth - 1) * 0.2;
            if (fontSize < 0.8)
            {
                fontSize = 0.8;
            }

            string html = $"<span class=\"mb-0\" style=\"font-size: {fontSize}rem;\"><a name=\"{link}\">{text}</a></span>\r\n";
            return new HandlerResult(html);
        }
    }
}
