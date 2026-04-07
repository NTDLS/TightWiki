namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Table of contents tag.
    /// </summary>
    public class TwTableOfContentsTag
    {
        public int Level { get; set; }
        public string HrefTag { get; set; }
        public string Text { get; set; }
        public int StartingPosition { get; set; }

        public TwTableOfContentsTag(int level, int startingPosition, string hrefTag, string text)
        {
            Level = level;
            StartingPosition = startingPosition;
            HrefTag = hrefTag;
            Text = text;
        }
    }
}
