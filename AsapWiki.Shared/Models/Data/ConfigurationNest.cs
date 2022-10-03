using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsapWiki.Shared.Models
{
    public class ConfigurationNest
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public List<ConfigurationEntry> Entries = new List<ConfigurationEntry>();
    }
}
