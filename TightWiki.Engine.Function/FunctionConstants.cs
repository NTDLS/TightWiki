namespace TightWiki.Engine.Function
{
    public class FunctionConstants
    {
        public enum WikiFunctionType
        {
            Standard,
            Scoped,
            Instruction
        }

        public enum WikiFunctionParamType
        {
            Undefined,
            String,
            InfiniteString,
            Integer,
            Double,
            Boolean
        }
    }
}
