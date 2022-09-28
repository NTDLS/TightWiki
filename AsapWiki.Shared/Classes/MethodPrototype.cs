using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsapWiki.Shared.Classes
{
    public class MethodPrototype
    {
        public string MethodName { get; set; }
        public List<PrototypeParameter> Parameters { get; set; }

        public MethodPrototype()
        {
            Parameters = new List<PrototypeParameter>();
        }
        
    }
}
