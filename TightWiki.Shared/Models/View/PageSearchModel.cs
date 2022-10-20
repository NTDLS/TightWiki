using System.Collections.Generic;
using TightWiki.Shared.Models.Data;

namespace TightWiki.Shared.Models.View
{
    public class PageSearchModel
    {
        public List<Page> Pages { get; set; }

        public string SearchTokens { get; set; }
    }
}
