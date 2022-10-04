using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpWiki.Shared.Wiki
{
    //Table of contense tag.
    public class TOCTag
    {
        public int Level;
        public string HrefTag;
        public string Text;
        public int StartingPosition;

        public TOCTag(int level, int startingPosition, string hrefTag, string text)
        {
            Level = level;
            StartingPosition = startingPosition;
            HrefTag = hrefTag;
            Text = text;
        }
    }
}
