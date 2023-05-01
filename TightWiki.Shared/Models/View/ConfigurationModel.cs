using System.Collections.Generic;
using TightWiki.Shared.Library;
using TightWiki.Shared.Models.Data;

namespace TightWiki.Shared.Models.View
{
    public class ConfigurationModel : ModelBase
    {
        public List<Role> Roles { get; set; }
        public List<TimeZoneItem> TimeZones { get; set; }
        public List<CountryItem> Countries { get; set; }
        public List<LanguageItem> Languages { get; set; }
        public List<ConfigurationNest> Nest { get; set; } = new List<ConfigurationNest>();

    }
}