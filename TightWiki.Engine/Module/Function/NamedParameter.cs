namespace TightWiki.Engine.Module.Function
{
    public class NamedParameter(string name, object? value)
    {
        public string Name { get; set; } = name;
        public object? Value { get; set; } = value;
    }
}
