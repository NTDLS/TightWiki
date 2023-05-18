using System.Collections.Generic;
using TightWiki.Shared.Models.Data;

namespace TightWiki.Shared.Models.View
{
    public class PageCommentsModel : ModelBase
    {
        public List<PageComment> Comments { get; set; }
        public string Comment { get; set; }
    }
}
