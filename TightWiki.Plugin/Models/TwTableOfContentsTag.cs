namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents a single entry in a wiki page's table of contents,
    /// capturing the heading level, anchor tag, display text, and position within the page body.
    /// </summary>
    public class TwTableOfContentsTag
    {
        /// <summary>
        /// The heading level of this table of contents entry, where 1 is the topmost level.
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// The HTML anchor tag used to link to this heading within the page.
        /// </summary>
        public string HrefTag { get; set; }

        /// <summary>
        /// The display text of this table of contents entry, derived from the heading content.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// The character position within the page body where this heading begins.
        /// </summary>
        public int StartingPosition { get; set; }

        /// <summary>
        /// Initializes a new table of contents entry with the specified level, position, anchor tag, and display text.
        /// </summary>
        /// <param name="level">The heading level of this entry.</param>
        /// <param name="startingPosition">The character position within the page body where this heading begins.</param>
        /// <param name="hrefTag">The HTML anchor tag used to link to this heading.</param>
        /// <param name="text">The display text of this entry.</param>
        public TwTableOfContentsTag(int level, int startingPosition, string hrefTag, string text)
        {
            Level = level;
            StartingPosition = startingPosition;
            HrefTag = hrefTag;
            Text = text;
        }
    }
}