using SharpWiki.Shared.Models.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpWiki.Shared.Models.View
{
    public class PageHistoryModel
    {
        public List<PageRevisionHistory> History { get; set; }
    }
}
