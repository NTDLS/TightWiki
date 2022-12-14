using System.Collections.Generic;
using TightWiki.Shared.Models.Data;

namespace TightWiki.Shared.Models.View
{
    public class PageHistoryModel : ModelBase
    {
        public List<PageRevisionHistory> History { get; set; }
    }
}
