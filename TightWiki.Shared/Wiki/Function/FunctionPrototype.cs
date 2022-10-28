using System.Collections.Generic;

namespace TightWiki.Shared.Wiki.Function
{
    public class FunctionPrototype
    {
        public string FunctionPrefix { get; set; }
        public string ProperName { get; set; }
        public string FunctionName { get; set; }
        public List<PrototypeParameter> Parameters { get; set; }

        public FunctionPrototype()
        {
            Parameters = new List<PrototypeParameter>();
        }
    }
}
