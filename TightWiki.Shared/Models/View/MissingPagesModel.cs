using System.Collections.Generic;
using TightWiki.Shared.Models.Data;

namespace TightWiki.Shared.Models.View
{
    public class MissingPagesModel : ModelBase
    {
        public List<NonexistentPage> Pages { get; set; }
    }
}
