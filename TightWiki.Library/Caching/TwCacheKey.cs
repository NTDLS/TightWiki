using TightWiki.Plugin.Caching;
using static TightWiki.Library.Caching.TwCache;

namespace TightWiki.Library.Caching
{
    /// <summary>
    /// Contains a verbatim cache key.
    /// </summary>
    /// <param name="key"></param>
    public class TwCacheKey(string key)
        : ITwCacheKey
    {
        public string Key { get; set; } = key;

        public static TwCacheKey Build(Category category, object?[] segments)
            => new($"[{category}]:[{string.Join("]:[", segments)}]");

        public static TwCacheKey Build(Category category)
            => new($"[{category}]");
    }
}
