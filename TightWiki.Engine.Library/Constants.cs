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
            //Do nothing.
            Skip,
            //Kill single trailing newline after match.
            KillTrailingLine,
            DisallowNestedDecode
        }
    }
}
