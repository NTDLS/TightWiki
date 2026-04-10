using System.Text.RegularExpressions;

namespace TightWiki.Plugin
{
    internal static partial class PrecompiledRegex
    {
        [GeneratedRegex(@"(##|{{|@@)([a-zA-Z_\s{][a-zA-Z0-9_\s{]*)\(((?<BR>\()|(?<-BR>\))|[^()]*)+\)")]
        public static partial Regex FunctionCallParser();
    }
}