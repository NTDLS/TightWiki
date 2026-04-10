using System.Diagnostics.CodeAnalysis;
using TightWiki.Plugin.Function;
using TightWiki.Plugin.Interfaces.Module.Function;

namespace TightWiki.Engine
{
    public static class Extensions
    {
        public static bool TryGetFunctionDescriptor(this List<ITwFunctionDescriptor> list,
            ParsedFunction parsed, [NotNullWhen(true)] out ITwFunctionDescriptor? found)
        {
            found = list.FirstOrDefault(o => o.FunctionAttribute.Demarcation == parsed.Demarcation
                && o.Method.Name.Equals(parsed.Name, StringComparison.InvariantCultureIgnoreCase));
            return found != null;
        }
    }
}
