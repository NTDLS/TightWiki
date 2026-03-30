using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace TightWiki.Engine
{
    internal class EmptyQueryCollection
        : Dictionary<string, StringValues>, IQueryCollection
    {
        public new StringValues this[string key] => TryGetValue(key, out var value) ? value : StringValues.Empty;

        ICollection<string> IQueryCollection.Keys => Keys;
    }
}
