using System.Collections.Generic;
using TightWiki.Shared.Models.Data;

namespace TightWiki.Shared.Models.View
{
    public class EmojisModel : ModelBase
    {
        public List<Emoji> Emojis { get; set; }
        public string Categories { get; set; }
    }
}
