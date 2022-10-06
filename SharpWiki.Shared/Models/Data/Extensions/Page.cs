using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SharpWiki.Shared.Models
{
    public partial class Page
    {
        public int TokenWeight { get; set; }
        public string Body { get; set; }
        public string CreatedByUserName { get; set; }
        public string ModifiedByUserName { get; set; }
    }
}
