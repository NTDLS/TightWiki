namespace TightWiki.Plugin.Engine.Function
{
    public class TwNamedParameter
    {
        public string Name { get; set; }
        public object? Value { get; set; }

        public TwNamedParameter(string name, object? value)
        {
            Name = name;
            Value = value;
        }
    }
}
