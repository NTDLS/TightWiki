using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TightWiki.Shared.Models.Data;

namespace TightWiki.Shared.Models.View
{
    public class PagesModel
    {
        public List<Page> Pages { get; set; }

        public string SearchTokens { get; set; }
    }
}
