using System.Diagnostics.CodeAnalysis;
using TightWiki.Engine.Library.Function;

namespace TightWiki.Engine.Library
{
    public static class Extensions
    {
        public static bool TryGetFunctionDescriptor(this List<TightEngineFunctionDescriptor> list,
            ParsedFunctionCall parsed, [NotNullWhen(true)] out TightEngineFunctionDescriptor? found)
        {
            found = list.FirstOrDefault(o => o.Attribute.Demarcation == parsed.Demarcation
                && o.Method.Name.Equals(parsed.Name, StringComparison.InvariantCultureIgnoreCase));
            return found != null;
        }
    }
}
