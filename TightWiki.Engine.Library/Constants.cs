namespace TightWiki.Engine.Library
{
    public class Constants
    {
        public const string SoftBreak = "<!--SoftBreak-->"; //These will remain as \r\n in the final HTML.
        public const string HardBreak = "<!--HardBreak-->"; //These will remain as <br /> in the final HTML.

        public enum WikiMatchType
        {
            Block,
            Instruction,
            Comment,
            Variable,
            Formatting,
            Error,
            Function,
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
    }
}
