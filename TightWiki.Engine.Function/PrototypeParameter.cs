using static TightWiki.Engine.Function.FunctionPrototypeCollection;

namespace TightWiki.Engine.Function
{
    public class PrototypeParameter
    {
        public WikiFunctionParamType Type { get; set; } = WikiFunctionParamType.Undefined;
        public string Name { get; set; } = string.Empty;
        public string? DefaultValue { get; set; } = string.Empty;
        public bool IsRequired { get; set; } = false;
        public bool IsInfinite { get; private set; } = false;
        public List<string> AllowedValues { get; set; } = new();

        public PrototypeParameter(WikiFunctionParamType type, string name)
        {
            Type = type;
            Name = name;

            if (type == WikiFunctionParamType.InfiniteString)
            {
                //We use IsInfinite instead of just checking the type for the case where
                //  we eventually introduce other "infinite" types such as InfiniteInteger
                IsInfinite = true;
            }
        }
    }
}
