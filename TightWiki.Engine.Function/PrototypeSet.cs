namespace TightWiki.Engine.Function
{
    public class PrototypeSet
    {
        public string Demarcation { get; set; } = string.Empty;
        public string ProperName { get; set; } = string.Empty;
        public string FunctionName { get; set; } = string.Empty;
        public FunctionPrototype Value { get; set; } = new();
    }
}
