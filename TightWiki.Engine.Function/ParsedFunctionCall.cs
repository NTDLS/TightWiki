﻿namespace TightWiki.Engine.Function
{
    public class ParsedFunctionCall
    {
        public string Prefix { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int EndIndex { get; set; }
        public List<string> RawArguments { get; set; } = new List<string>();

        public ParsedFunctionCall(string prefix, string name, int endIndex, List<string> rawArguments)
        {
            Prefix = prefix;
            Name = name;
            EndIndex = endIndex;
            RawArguments = rawArguments;
        }
    }
}
