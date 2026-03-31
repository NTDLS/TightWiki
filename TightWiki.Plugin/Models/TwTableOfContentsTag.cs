namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Table of contents tag.
    /// </summary>
    public class TwTableOfContentsTag
    {
        public int Level;
        public string HrefTag;
        public string Text;
        public int StartingPosition;

        public TwTableOfContentsTag(int level, int startingPosition, string hrefTag, string text)
        {
            Level = level;
            StartingPosition = startingPosition;
            HrefTag = hrefTag;
            Text = text;
        }
    }
}
