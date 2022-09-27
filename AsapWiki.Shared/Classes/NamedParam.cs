using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsapWiki.Shared.Classes
{
    public class NamedParam
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public NamedParam(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }
}
