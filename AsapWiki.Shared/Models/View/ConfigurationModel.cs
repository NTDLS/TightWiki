using System.Collections.Generic;

namespace AsapWiki.Shared.Models.View
{
    public class ConfigurationModel
    {
        public List<ConfigurationNest> Nest { get; set; } = new List<ConfigurationNest>();

    }
}