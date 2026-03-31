using TightWiki.Plugin.Caching;
using static TightWiki.Library.Caching.MemCache;

namespace TightWiki.Library.Caching
{
    /// <summary>
    /// Contains a verbatim cache key.
    /// </summary>
    /// <param name="key"></param>
    public class MemCacheKey(string key)
        : ITwCacheKey
    {
        public string Key { get; set; } = key;

        public static MemCacheKey Build(Category category, object?[] segments)
            => new($"[{category}]:[{string.Join("]:[", segments)}]");

        public static MemCacheKey Build(Category category)
            => new($"[{category}]");
    }
}
