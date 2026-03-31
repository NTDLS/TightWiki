using System.Diagnostics.CodeAnalysis;
using TightWiki.Engine.Module.Function;
using TightWiki.Plugin.Interfaces.Module.Function;

namespace TightWiki.Engine
{
    public static class Extensions
    {
        public static bool TryGetFunctionDescriptor(this List<ITwFunctionDescriptor> list,
            ParsedFunction parsed, [NotNullWhen(true)] out ITwFunctionDescriptor? found)
        {
            found = list.FirstOrDefault(o => o.Attribute.Demarcation == parsed.Demarcation
                && o.Method.Name.Equals(parsed.Name, StringComparison.InvariantCultureIgnoreCase));
            return found != null;
        }
    }
}
