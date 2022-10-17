using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TightWiki.Shared.Models.Data.Extensions
{
    public partial class User
    {
        public int PaginationSize { get; set; }
        public int PaginationCount { get; set; }
    }
}

