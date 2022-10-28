namespace TightWiki.Shared.Wiki.Function
{
    public class NamedParam
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public NamedParam(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }
}
