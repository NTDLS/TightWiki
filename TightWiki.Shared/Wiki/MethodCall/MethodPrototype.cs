using System.Collections.Generic;

namespace TightWiki.Shared.Wiki.MethodCall
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
