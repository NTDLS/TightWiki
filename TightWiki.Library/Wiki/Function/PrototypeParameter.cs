using System.Collections.Generic;

namespace TightWiki.Library.Wiki.Function
{
    public class PrototypeParameter
    {
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DefaultValue { get; set; } = string.Empty;
        public bool IsRequired { get; set; } = false;
        public bool IsInfinite { get; set; } = false;
        public List<string> AllowedValues { get; set; } = new();
    }
}
