using System.Runtime.CompilerServices;
using static TightWiki.Library.WikiCache;

namespace TightWiki.Library
{
    /// <summary>
    /// Contains a verbatim cache key which also includes the calling function name.
    /// </summary>
    /// <param name="key"></param>
    public class WikiCacheKeyFunction(string key)
    {
        public string Key { get; set; } = key;

        /// <summary>
        /// Builds a cache key which includes the calling function name.
        /// </summary>
        public static WikiCacheKeyFunction Build(Category category, object?[] segments, [CallerMemberName] string callingFunction = "")
            => new($"[{category}]:[{string.Join("]:[", segments)}]:[{callingFunction}]");

        /// <summary>
        /// Builds a cache key which includes the calling function name.
        /// </summary>
        public static WikiCacheKeyFunction Build(Category category, [CallerMemberName] string callingFunction = "")
            => new($"[{category}]:[{callingFunction}]");
    }
}
