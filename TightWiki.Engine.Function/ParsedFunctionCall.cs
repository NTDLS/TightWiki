namespace TightWiki.Engine.Function
{
    public class ParsedFunctionCall
    {
        public string Demarcation { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int EndIndex { get; set; }
        public List<string> Arguments { get; set; } = new List<string>();

        public ParsedFunctionCall(string demarcation, string name, int endIndex, List<string> arguments)
        {
            Demarcation = demarcation;
            Name = name;
            EndIndex = endIndex;
            Arguments = arguments;
        }
    }
}
