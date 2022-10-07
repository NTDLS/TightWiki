using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpWiki.Shared.Models.Data
{
    public class PageToken
    {
        public int PageId { get; set; }
        public string Token { get; set; }
        public int Weight { get; set; }
    }
}
