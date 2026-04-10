using System.Text.RegularExpressions;

namespace TightWiki.Plugin
{
    public static partial class PrecompiledRegex
    {
        [GeneratedRegex("{{([\\S\\s]*)}}", RegexOptions.IgnoreCase)]
        public static partial Regex ScopeFunctionBlock();

        [GeneratedRegex("(\\#\\#[\\w-]+\\(\\))|(\\#\\#)([a-zA-Z_\\s{][a-zA-Z0-9_\\s{]*)\\(((?<BR>\\()|(?<-BR>\\))|[^()]*)+\\)|(\\#\\#[\\w-]+)", RegexOptions.IgnoreCase)]
        public static partial Regex StandardFunctionBlock();

        [GeneratedRegex("(\\@\\@[\\w-]+\\(\\))|(\\@\\@[\\w-]+\\(.*?\\))|(\\@\\@[\\w-]+)", RegexOptions.IgnoreCase)]
        public static partial Regex ProcessingInstructionBlock();

        [GeneratedRegex("(\\#\\#[\\w-]+\\(\\))|(##|{{|@@)([a-zA-Z_\\s{][a-zA-Z0-9_\\s{]*)\\(((?<BR>\\()|(?<-BR>\\))|[^()]*)+\\)|(\\#\\#[\\w-]+)", RegexOptions.IgnoreCase)]
        public static partial Regex PostProcessBlock();
    }
}
