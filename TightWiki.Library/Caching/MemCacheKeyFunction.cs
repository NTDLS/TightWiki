using System.Runtime.CompilerServices;
using TightWiki.Plugin.Caching;
using static TightWiki.Library.Caching.MemCache;

namespace TightWiki.Library.Caching
{
    /// <summary>
    /// Contains a verbatim cache key which also includes the calling function name.
    /// </summary>
    /// <param name="key"></param>
    public class MemCacheKeyFunction(string key)
        : ITwCacheKey
    {
        public string Key { get; set; } = key;

        /// <summary>
        /// Builds a cache key which includes the calling function name.
        /// </summary>
        public static MemCacheKeyFunction Build(Category category, object?[] segments, [CallerMemberName] string callingFunction = "")
            => new($"[{category}]:[{string.Join("]:[", segments)}]:[{callingFunction}]");

        /// <summary>
        /// Builds a cache key which includes the calling function name.
        /// </summary>
        public static MemCacheKeyFunction Build(Category category, [CallerMemberName] string callingFunction = "")
            => new($"[{category}]:[{callingFunction}]");
    }
}
