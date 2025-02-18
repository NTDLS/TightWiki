namespace TightWiki.Engine.Function
{
    public class FunctionPrototype
    {
        public string ProperName { get; set; } = string.Empty;
        public string FunctionName { get; set; } = string.Empty;
        public List<PrototypeParameter> Parameters { get; set; }

        public FunctionPrototype(string properName)
        {
            Parameters = new List<PrototypeParameter>();
            ProperName = properName;
            FunctionName = properName.ToLowerInvariant();
        }

        public FunctionPrototype()
        {
            Parameters = new List<PrototypeParameter>();
        }
    }
}
