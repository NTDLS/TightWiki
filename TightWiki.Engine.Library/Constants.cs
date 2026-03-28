using static TightWiki.Engine.Library.Function.FunctionConstants;

namespace TightWiki.Engine.Library
{
    public static class Constants
    {
        public static WikiFunctionType ParseDemarcation(string demarcation)
        {
            return demarcation switch
            {
                "##" => WikiFunctionType.Standard,
                "{{" => WikiFunctionType.Scoped,
                "@@" => WikiFunctionType.Instruction,
                _ => throw new Exception("Invalid demarcation string."),
            };
        }

        public const string SoftBreak = "<!--SoftBreak-->"; //These will remain as \r\n in the final HTML.
        public const string HardBreak = "<!--HardBreak-->"; //These will remain as <br /> in the final HTML.
    }

    public enum WikiMatchType
    {
        ScopeFunction,
        Emoji,
        Instruction,
        Comment,
        Variable,
        Markup,
        Error,
        StandardFunction,
        Link,
        Heading,
        Literal
    }

    public enum HandlerResultInstruction
    {
        /// <summary>
        /// Does not process the match, allowing it to be processed by another handler.
        /// </summary>
        Skip,
        /// <summary>
        /// Removes any single trailing newline after match.
        /// </summary>
        TruncateTrailingLine,
        /// <summary>
        /// Will not continue to process content in this block.
        /// </summary>
        DisallowNestedProcessing,
        /// <summary>
        /// As opposed to the default functionality of replacing all matches, this will cause ony the first match to be replaced.
        /// This also means that each match will be processed individually, which can impact performance.
        /// </summary>
        OnlyReplaceFirstMatch
    }

    public enum TightWikiAlignStyle
    {
        Default,
        Start,
        Center,
        End
    }

    public enum TightWikiBootstrapStyle
    {
        Default,
        Primary,
        Secondary,
        Light,
        Dark,
        Success,
        Info,
        Warning,
        Danger,
        Muted,
        White
    }

    public enum TightWikiOrder
    {
        Ascending,
        Descending,
    }

    public enum TightWikiBulletStyle
    {
        Ordered,
        Unordered
    }

    public enum TightWikiListStyle
    {
        Full,
        List
    }

    public enum TightWikiTabularStyle
    {
        Full,
        List,
        Flat
    }

    public enum TightWikiLinkStyle
    {
        Text,
        Link,
        LinkName
    }

    public enum TightWikiCodeLanguage
    {
        Auto,
        Wiki,
        Cpp,
        Lua,
        GraphQL,
        Swift,
        R,
        Yaml,
        Kotlin,
        Scss,
        Shell,
        Vbnet,
        Json,
        ObjectiveC,
        Perl,
        Diff,
        Wasm,
        PHP,
        Xml,
        Bash,
        CSharp,
        CSS,
        GO,
        Ini,
        JavaScript,
        Less,
        Makefile,
        Markdown,
        PlainText,
        Python,
        Ruby,
        Rust,
        Sql,
        Typescript
    }
}
