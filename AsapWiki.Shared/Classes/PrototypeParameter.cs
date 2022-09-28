using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsapWiki.Shared.Classes
{
    public class PrototypeParameter
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public string DefaultValue { get; set; }
        public bool IsRequired { get; set; } = false;
        public bool IsInfinite { get; set; } = false;
        public List<string> AllowedValues { get; set; }
    }
}
