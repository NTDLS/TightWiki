namespace TightWiki.Plugin.Engine.Function
{
    public class NamedParameter
    {
        public string Name { get; set; }
        public object? Value { get; set; }

        public NamedParameter(string name, object? value)
        {
            Name = name;
            Value = value;
        }
    }
}
