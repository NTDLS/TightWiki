namespace TightWiki.Shared.Wiki.MethodCall
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
