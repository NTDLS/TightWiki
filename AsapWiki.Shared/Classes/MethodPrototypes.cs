using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsapWiki.Shared.Classes
{
    public class MethodPrototypes
    {
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
