using TightWiki.Wiki.Function;
using static TightWiki.Library.Constants;

namespace TightWiki.Engine
{
    public class MatchSet
    {
        /// <summary>
        /// The type of match that was found.
        /// </summary>
        public WikiMatchType MatchType { get; set; }

        /// <summary>
        /// The resulting content of the wiki processing.
        /// </summary>
        public string Content { get; set; } = string.Empty;
        /// <summary>
        /// The content in this segment will be wikified. This is useful to disable on things like error messages
        /// and literal blocks where the content may contain valid wiki markup but we want it to display verbatim.
        /// </summary>
        public bool AllowNestedDecode { get; set; }

        /// <summary>
        /// The function call that resulted in this match.
        /// </summary>
        public FunctionCall? Function { get; set; }
    }
}
