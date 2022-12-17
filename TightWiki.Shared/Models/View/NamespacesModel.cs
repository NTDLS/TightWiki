using System.Collections.Generic;
using TightWiki.Shared.Models.Data;

namespace TightWiki.Shared.Models.View
{
    public class NamespacesModel : ModelBase
    {
        public List<NamespaceStat> Namespaces { get; set; }
    }
}
