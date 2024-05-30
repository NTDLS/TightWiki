using static TightWiki.Library.WikiCache;

namespace TightWiki.Library
{
    /// <summary>
    /// Contains a verbatim cache key.
    /// </summary>
    /// <param name="key"></param>
    public class WikiCacheKey(string key)
    {
        public string Key { get; set; } = key;

        public static WikiCacheKey Build(Category category, object?[] segments)
            => new($"[{category}]:[{string.Join("]:[", segments)}]");

        public static WikiCacheKey Build(Category category)
            => new($"[{category}]");
    }
}
