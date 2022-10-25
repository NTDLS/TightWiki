using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TightWiki.Shared.Models.Data;

namespace TightWiki.Shared.Models.View
{
    public class RoleModel : ModelBase
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<User> Users { get; set; }
    }
}
