using TightWiki.Engine.Library;
using TightWiki.Engine.Library.Interfaces;
using static TightWiki.Engine.Library.Constants;

namespace TightWiki.Engine.Implementation
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
            if (depth >= 2 && depth <= 6)
            {
                int fontSize = 8 - depth;
                if (fontSize < 5) fontSize = 5;

                string html = "<font size=\"" + fontSize + "\"><a name=\"" + link + "\"><span class=\"WikiH" + (depth - 1).ToString() + "\">" + text + "</span></a></font>\r\n";
                return new HandlerResult(html);
            }

            return new HandlerResult() { Instructions = [HandlerResultInstruction.Skip] };
        }
    }
}
