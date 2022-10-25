using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TightWiki.Shared.Library;
using TightWiki.Shared.Models.Data;

namespace TightWiki.Shared.Models.View
{
    public class AccountModel : ModelBase
    {
        public List<TimeZoneItem> TimeZones { get; set; }
        public List<CountryItem> Countries { get; set; }
        public List<LanguageItem> Languages { get; set; }
        public User Account { get; set; }
    }
}
