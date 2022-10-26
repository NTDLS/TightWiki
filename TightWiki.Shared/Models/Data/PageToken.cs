using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TightWiki.Shared.Models.Data
{
    public class PageToken
    {
        public int PageId { get; set; }
        public string Token { get; set; }
        public string DoubleMetaphone { get; set; }
        public int Weight { get; set; }
    }
}
