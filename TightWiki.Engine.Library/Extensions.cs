using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TightWiki.Engine.Library.Function;

namespace TightWiki.Engine.Library
{
    public static class Extensions
    {
        public static bool IsInfinite(this ParameterInfo param)
        {
            return param.ParameterType.IsArray //It the parameter an array or List<>?
                    || (param.ParameterType.IsGenericType && param.ParameterType.GetGenericTypeDefinition() == typeof(List<>));
        }

        public static bool TryGetFunctionEnvelope(this List<TightEngineFunctionEnvelope> list,
            ParsedFunctionCall parsed, [NotNullWhen(true)] out TightEngineFunctionEnvelope? found)
        {
            found = list.FirstOrDefault(o => o.Attribute.Demarcation == parsed.Demarcation
                && o.Method.Name.Equals(parsed.Name, StringComparison.InvariantCultureIgnoreCase));
            return found != null;
        }
    }
}
