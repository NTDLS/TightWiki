using TightWiki.Shared.Models.Data;
using System.Collections.Generic;

namespace TightWiki.Shared.Models.View
{
    public class PageHistoryModel
    {
        public List<PageRevisionHistory> History { get; set; }
    }
}
