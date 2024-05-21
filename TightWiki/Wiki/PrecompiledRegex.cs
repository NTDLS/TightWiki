using System.Text.RegularExpressions;

namespace TightWiki.Wiki
{
    internal static partial class PrecompiledRegex
    {
        [GeneratedRegex("\\#\\{([\\S\\s]*?)\\}\\#", RegexOptions.IgnoreCase)]
        internal static partial Regex TransformLiterals();

        [GeneratedRegex("{{([\\S\\s]*)}}", RegexOptions.IgnoreCase)]
        internal static partial Regex TransformBlock();

        [GeneratedRegex("\\;\\;.*", RegexOptions.IgnoreCase)]
        internal static partial Regex TransformComments();

        [GeneratedRegex("(\\%\\%.+?\\%\\%)", RegexOptions.IgnoreCase)]
        internal static partial Regex TransformEmoji();

        [GeneratedRegex("(\\$\\{.+?\\})", RegexOptions.IgnoreCase)]
        internal static partial Regex TransformVariables();

        [GeneratedRegex("(\\[\\[http\\:\\/\\/.+?\\]\\])", RegexOptions.IgnoreCase)]
        internal static partial Regex TransformExplicitHTTPLinks();

        [GeneratedRegex("(\\[\\[https\\:\\/\\/.+?\\]\\])", RegexOptions.IgnoreCase)]
        internal static partial Regex TransformExplicitHTTPsLinks();

        [GeneratedRegex("(\\[\\[.+?\\]\\])", RegexOptions.IgnoreCase)]
        internal static partial Regex TransfomInternalDynamicLinks();

        [GeneratedRegex("(\\#\\#[\\w-]+\\(\\))|(##|{{|@@)([a-zA-Z_\\s{][a-zA-Z0-9_\\s{]*)\\(((?<BR>\\()|(?<-BR>\\))|[^()]*)+\\)|(\\#\\#[\\w-]+)", RegexOptions.IgnoreCase)]
        internal static partial Regex TransformFunctions();

        [GeneratedRegex("(\\@\\@[\\w-]+\\(\\))|(\\@\\@[\\w-]+\\(.*?\\))|(\\@\\@[\\w-]+)", RegexOptions.IgnoreCase)]
        internal static partial Regex TransformProcessingInstructions();

        [GeneratedRegex("(\\#\\#[\\w-]+\\(\\))|(##|{{|@@)([a-zA-Z_\\s{][a-zA-Z0-9_\\s{]*)\\(((?<BR>\\()|(?<-BR>\\))|[^()]*)+\\)|(\\#\\#[\\w-]+)", RegexOptions.IgnoreCase)]
        internal static partial Regex TransformPostProcess();

        [GeneratedRegex("(=======.*?\n)|(======.*?\n)|(=====.*?\n)|(====.*?\n)|(===.*?\n)|(==.*?\n)", RegexOptions.IgnoreCase)]
        internal static partial Regex TransformSectionHeadings();

        [GeneratedRegex("(\\^\\^\\^\\^\\^\\^\\^.*?\n)|(\\^\\^\\^\\^\\^\\^.*?\n)|(\\^\\^\\^\\^\\^.*?\n)|(\\^\\^\\^\\^.*?\n)|(\\^\\^\\^.*?\n)|(\\^\\^.*?\n)", RegexOptions.IgnoreCase)]
        internal static partial Regex TransformHeaderMarkup();
    }
}
