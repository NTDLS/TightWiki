using TightWiki.Shared.Models.Data;
using System.Collections.Generic;

namespace TightWiki.Shared.Models.View
{
    public class ConfigurationModel
    {
        public List<ConfigurationNest> Nest { get; set; } = new List<ConfigurationNest>();

    }
}