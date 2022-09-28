using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsapWiki.Shared.Classes
{
    public class MethodPrototypes
    {
        //<type> //Only supports string, int, float, bool and string:infinite, int:infinite, and float:infinite.
        //{Optional (allowed|values) }='Default Value' or [Required (allowed|values) ]='Default Value'
        //All required parameters must come before the optional parameters.

        //... indicates infinite parameters, these should typically come after required parameters but
        //  can come before optional parameters as long as the optional parameters are passed by name.

        //prototype = "<string>[boxType(bullets,bullets-ordered)] | <string>{title}='' | ...";

        private class PrototypeSet
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }

        private List<PrototypeSet> Formulas { get; set; } = new List<PrototypeSet>();

        public void Add(string formula)
        {
            int nameEndIndex = formula.IndexOf(':');

            Formulas.Add(new PrototypeSet()
            {
                Name = formula.Substring(0, nameEndIndex).Trim().ToLower(),
                Value = formula
            }); ;
        }

        public string Get(string name)
        {
            return Formulas.Where(o => o.Name == name.ToLower()).FirstOrDefault()?.Value;
        }
    }
}
