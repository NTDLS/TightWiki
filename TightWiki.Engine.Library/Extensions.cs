using System.Diagnostics.CodeAnalysis;
using TightWiki.Engine.Library.Function;

namespace TightWiki.Engine.Library
{
    public static class Extensions
    {
        public static bool TryGetFunctionEnvelope(this List<TightEngineFunctionEnvelope> list,
            ParsedFunctionCall parsed, [NotNullWhen(true)] out TightEngineFunctionEnvelope? found)
        {
            found = list.FirstOrDefault(o => o.Attribute.Demarcation == parsed.Demarcation
                && o.Method.Name.Equals(parsed.Name, StringComparison.InvariantCultureIgnoreCase));
            return found != null;
        }
    }
}
