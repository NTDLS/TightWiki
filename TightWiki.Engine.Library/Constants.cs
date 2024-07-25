namespace TightWiki.Engine.Library
{
    public class Constants
    {
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
            KillTrailingLine
        }
    }
}
