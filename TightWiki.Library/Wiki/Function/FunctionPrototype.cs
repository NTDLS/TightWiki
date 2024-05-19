using System.Collections.Generic;

namespace TightWiki.Library.Wiki.Function
{
    public class FunctionPrototype
    {
        public string FunctionPrefix { get; set; } = string.Empty;
        public string ProperName { get; set; } = string.Empty;
        public string FunctionName { get; set; } = string.Empty;
        public List<PrototypeParameter> Parameters { get; set; }

        public FunctionPrototype()
        {
            Parameters = new List<PrototypeParameter>();
        }
    }
}
