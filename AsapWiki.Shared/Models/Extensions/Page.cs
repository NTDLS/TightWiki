using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AsapWiki.Shared.Models
{
    public partial class Page : BaseModel
    {
        public int TokenWeight { get; set; }
    }
}