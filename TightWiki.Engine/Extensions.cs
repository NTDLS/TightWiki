using System.Diagnostics.CodeAnalysis;
using TightWiki.Plugin.Engine.Function;

namespace TightWiki.Engine
{
    public static class Extensions
    {
        public static bool TryGetFunctionDescriptor(this List<TwEngineFunctionDescriptor> list,
            ParsedFunction parsed, [NotNullWhen(true)] out TwEngineFunctionDescriptor? found)
        {
            found = list.FirstOrDefault(o => o.Attribute.Demarcation == parsed.Demarcation
                && o.Method.Name.Equals(parsed.Name, StringComparison.InvariantCultureIgnoreCase));
            return found != null;
        }
    }
}
