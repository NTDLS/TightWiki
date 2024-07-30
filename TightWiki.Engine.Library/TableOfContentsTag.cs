namespace TightWiki.Engine.Library
{
    /// <summary>
    /// Table of contents tag.
    /// </summary>
    public class TableOfContentsTag
    {
        public int Level;
        public string HrefTag;
        public string Text;
        public int StartingPosition;

        public TableOfContentsTag(int level, int startingPosition, string hrefTag, string text)
        {
            Level = level;
            StartingPosition = startingPosition;
            HrefTag = hrefTag;
            Text = text;
        }
    }
}
