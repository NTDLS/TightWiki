namespace TightWiki.Engine.Function
{
    public class PrototypeSet
    {
        public string FunctionPrefix { get; set; } = string.Empty;
        public string ProperName { get; set; } = string.Empty;
        public string FunctionName { get; set; } = string.Empty;
        public FunctionPrototype Value { get; set; } = new();
    }
}
