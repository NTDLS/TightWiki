using System.Collections.Generic;

namespace TightWiki.Shared.Models.Data
{
    public class ConfigurationNest
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public List<ConfigurationEntry> Entries = new List<ConfigurationEntry>();
    }
}
