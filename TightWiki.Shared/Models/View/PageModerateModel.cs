using System.Collections.Generic;
using TightWiki.Shared.Models.Data;

namespace TightWiki.Shared.Models.View
{
    public class PageModerateModel : ModelBase
    {
        public List<string> Instructions { get; set; }
        public List<Page> Pages { get; set; }
        public string Instruction { get; set; }
    }
}
