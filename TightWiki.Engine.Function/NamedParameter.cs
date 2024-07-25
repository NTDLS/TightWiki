namespace TightWiki.EngineFunction
{
    public class NamedParameter
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public NamedParameter(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }
}
