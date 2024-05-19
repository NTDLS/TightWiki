using System.Collections.Generic;

namespace TightWiki.Library.DataModels
{
    public class ConfigurationNest
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public List<ConfigurationEntry> Entries = new List<ConfigurationEntry>();
    }
}
