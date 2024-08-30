using static TightWiki.Caching.WikiCache;

namespace TightWiki.Caching
{
    /// <summary>
    /// Contains a verbatim cache key.
    /// </summary>
    /// <param name="key"></param>
    public class WikiCacheKey(string key) : IWikiCacheKey
    {
        public string Key { get; set; } = key;

        public static WikiCacheKey Build(Category category, object?[] segments)
            => new($"[{category}]:[{string.Join("]:[", segments)}]");

        public static WikiCacheKey Build(Category category)
            => new($"[{category}]");
    }
}
