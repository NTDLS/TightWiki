using SharpWiki.Shared.Models.Data;
using System.Collections.Generic;

namespace SharpWiki.Shared.Models.View
{
    public class PageHistoryModel
    {
        public List<PageRevisionHistory> History { get; set; }
    }
}
