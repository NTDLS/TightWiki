using System.Collections.Specialized;
using System.Runtime.Caching;

namespace TightWiki.Library
{
    public class WikiCache
    {
        public enum Category
        {
            User,
            Page,
            Emoji,
            Configuration
        }

        const int DefaultCacheSeconds = 5 * 60;
        private static MemoryCache? _memcache;
        public static ulong CachePuts { get; set; }
        public static ulong CacheGets { get; set; }
        public static ulong CacheHits { get; set; }
        public static ulong CacheMisses { get; set; }
        public static int CacheItemCount => Memcache.Count();
        public static double CacheMemoryLimitMB => Memcache.CacheMemoryLimit / 1024.0 / 1024.0;

        public static MemoryCache Memcache
        {
            get
            {
                if (_memcache == null)
                {
                    var config = new NameValueCollection();
                    //config.Add("pollingInterval", "00:05:00");
                    //config.Add("physicalMemoryLimitPercentage", "0");
                    config.Add("CacheMemoryLimitMegabytes", GlobalSettings.CacheMemoryLimitMB.ToString());
                    _memcache = new MemoryCache("TightWikiCache", config);
                }
                return _memcache;
            }
        }

        /// <summary>
        /// Gets an item from the cache.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <returns></returns>
        public static T? Get<T>(WikiCacheKeyFunction cacheKey)
        {
            CacheGets++;
            var result = (T)Memcache.Get(cacheKey.Key);

            if (result == null)
            {
                CacheMisses++;
            }
            else
            {
                CacheHits++;
            }

            return result;
        }

        /// <summary>
        /// Adds an item to the cache. If the item is already in the cache, this will reset its expiration.
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="value"></param>
        /// <param name="seconds"></param>
        public static void Put(WikiCacheKeyFunction cacheKey, object value, int seconds = DefaultCacheSeconds)
        {
            CachePuts++;

            var policy = new CacheItemPolicy()
            {
                AbsoluteExpiration = System.DateTimeOffset.Now.AddSeconds(seconds)
            };
            Memcache.Add(cacheKey.Key, value, policy);
        }

        /// <summary>
        /// Removes all entries from the cache.
        /// </summary>
        public static void Clear()
        {
            var items = Memcache.ToList();
            foreach (var a in items)
            {
                Memcache.Remove(a.Key);
            }
        }

        /// <summary>
        /// Removes cache entries that begin with the given cache key.
        /// </summary>
        /// <param name="category"></param>
        public static void ClearCategory(WikiCacheKey cacheKey)
        {
            var keys = new List<string>();

            foreach (var item in Memcache)
            {
                if (item.Key.StartsWith(cacheKey.Key))
                {
                    keys.Add(item.Key);
                }
            }

            keys.ForEach(o => Memcache.Remove(o));
        }

        /// <summary>
        /// Removes cache entries in a given category.
        /// </summary>
        /// <param name="category"></param>
        public static void ClearCategory(Category category)
        {
            var cacheKey = WikiCacheKey.Build(category);

            var keys = new List<string>();

            foreach (var item in Memcache)
            {
                if (item.Key.StartsWith(cacheKey.Key))
                {
                    keys.Add(item.Key);
                }
            }

            keys.ForEach(o => Memcache.Remove(o));
        }
    }
}
