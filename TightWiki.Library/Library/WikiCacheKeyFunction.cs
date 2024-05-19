using System.Runtime.CompilerServices;
using static TightWiki.Library.Library.WikiCache;

namespace TightWiki.Library.Library
{
    /// <summary>
    /// Contains a verbatum cache key which also includes the calling function name.
    /// </summary>
    /// <param name="key"></param>
    public class WikiCacheKeyFunction(string key)
    {
        public string Key { get; set; } = key;

        public static WikiCacheKeyFunction Build(Category category, object?[] segments, [CallerMemberName] string callingFunction = "")
            => new($"[{category}]:[{string.Join("]:[", segments)}]:[{callingFunction}]");

        public static WikiCacheKeyFunction Build(Category category, [CallerMemberName] string callingFunction = "")
            => new($"[{category}]:[{callingFunction}]");
    }
}
