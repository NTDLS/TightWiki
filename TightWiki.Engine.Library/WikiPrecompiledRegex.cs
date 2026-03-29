using System.Text.RegularExpressions;

namespace TightWiki.Engine.Library
{
    public static partial class PrecompiledRegex
    {
        [GeneratedRegex(@"(##|{{|@@)([a-zA-Z_\s{][a-zA-Z0-9_\s{]*)\(((?<BR>\()|(?<-BR>\))|[^()]*)+\)")]
        public static partial Regex FunctionCallParser();

        [GeneratedRegex("\\#\\{([\\S\\s]*?)\\}\\#", RegexOptions.IgnoreCase)]
        public static partial Regex TransformLiterals();

        [GeneratedRegex("{{([\\S\\s]*)}}", RegexOptions.IgnoreCase)]
        public static partial Regex TransformBlock();

        [GeneratedRegex("\\;\\;.*", RegexOptions.IgnoreCase)]
        public static partial Regex TransformComments();

        [GeneratedRegex("(\\%\\%.+?\\%\\%)", RegexOptions.IgnoreCase)]
        public static partial Regex TransformEmoji();

        [GeneratedRegex("(\\$\\{.+?\\})", RegexOptions.IgnoreCase)]
        public static partial Regex TransformVariables();

        [GeneratedRegex("(\\[\\[http\\:\\/\\/.+?\\]\\])", RegexOptions.IgnoreCase)]
        public static partial Regex TransformExplicitHTTPLinks();

        [GeneratedRegex("(\\[\\[https\\:\\/\\/.+?\\]\\])", RegexOptions.IgnoreCase)]
        public static partial Regex TransformExplicitHTTPsLinks();

        [GeneratedRegex("(\\[\\[.+?\\]\\])", RegexOptions.IgnoreCase)]
        public static partial Regex TransformInternalDynamicLinks();

        [GeneratedRegex("(\\#\\#[\\w-]+\\(\\))|(\\#\\#)([a-zA-Z_\\s{][a-zA-Z0-9_\\s{]*)\\(((?<BR>\\()|(?<-BR>\\))|[^()]*)+\\)|(\\#\\#[\\w-]+)", RegexOptions.IgnoreCase)]
        public static partial Regex TransformFunctions();

        [GeneratedRegex("(\\@\\@[\\w-]+\\(\\))|(\\@\\@[\\w-]+\\(.*?\\))|(\\@\\@[\\w-]+)", RegexOptions.IgnoreCase)]
        public static partial Regex TransformProcessingInstructions();

        [GeneratedRegex("(\\#\\#[\\w-]+\\(\\))|(##|{{|@@)([a-zA-Z_\\s{][a-zA-Z0-9_\\s{]*)\\(((?<BR>\\()|(?<-BR>\\))|[^()]*)+\\)|(\\#\\#[\\w-]+)", RegexOptions.IgnoreCase)]
        public static partial Regex TransformPostProcess();

        [GeneratedRegex(@"^(={2,7}.*)", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
        public static partial Regex TransformSectionHeadings();

        [GeneratedRegex(@"^(\^{2,7}.*)", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
        public static partial Regex TransformHeaderMarkup();
    }
}
