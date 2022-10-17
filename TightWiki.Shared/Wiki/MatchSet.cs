using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TightWiki.Shared.Wiki
{
    public class MatchSet
    {
        public string Content { get; set; }
        /// <summary>
        /// The content in this segment will not be wikified.
        /// </summary>
        public bool AllowNestedDecode { get; set; }
    }
}
