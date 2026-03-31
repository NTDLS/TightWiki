using System.Diagnostics.CodeAnalysis;
using TightWiki.Plugin.Engine.Function;

namespace TightWiki.Engine
{
    public static class TwExtensions
    {
        public static bool TryGetFunctionDescriptor(this List<TwEngineFunctionDescriptor> list,
            TwParsedFunction parsed, [NotNullWhen(true)] out TwEngineFunctionDescriptor? found)
        {
            found = list.FirstOrDefault(o => o.Attribute.Demarcation == parsed.Demarcation
                && o.Method.Name.Equals(parsed.Name, StringComparison.InvariantCultureIgnoreCase));
            return found != null;
        }
    }
}
