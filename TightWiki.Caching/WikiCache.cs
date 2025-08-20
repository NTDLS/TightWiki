using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Caching;

namespace TightWiki.Caching
{
    public class WikiCache
    {
        public delegate T? GetValueDelegate<T>();

        public enum Category
        {
            User,
            Page,
            Security,
            Search,
            Emoji,
            Configuration
        }

        public static int DefaultCacheSeconds { get; set; }
        private static MemoryCache? _memCache;

        public static ulong CachePuts { get; set; }
        public static ulong CacheGets { get; set; }
        public static ulong CacheHits { get; set; }
        public static ulong CacheMisses { get; set; }
        public static int CacheItemCount => MemCache.Count();
        public static double CacheMemoryLimit => MemCache.CacheMemoryLimit;

        public static MemoryCache MemCache => _memCache ?? throw new Exception("Cache has not been initialized.");

        public static void Initialize(int cacheMemoryLimitMB, int defaultCacheSeconds)
        {
            DefaultCacheSeconds = defaultCacheSeconds;
            var config = new NameValueCollection
            {
                //config.Add("pollingInterval", "00:05:00");
                //config.Add("physicalMemoryLimitPercentage", "0");
                { "CacheMemoryLimitMegabytes", cacheMemoryLimitMB.ToString() }
            };
            _memCache?.Dispose();
            _memCache = new MemoryCache("TightWikiCache", config);
        }

        /// <summary>
        /// Gets an item from the cache.
        /// </summary>
        public static T? Get<T>(IWikiCacheKey cacheKey)
        {
            CacheGets++;
            var result = (T)MemCache.Get(cacheKey.Key);

            if (result == null)
            {
                CacheMisses++;
            }

            CacheHits++;
            return result;
        }

        /// <summary>
        /// Determines if the cache contains a given key.
        /// </summary>
        public static bool Contains(IWikiCacheKey cacheKey)
        {
            CacheGets++;
            if (MemCache.Contains(cacheKey.Key))
            {
                CacheHits++;
                return true;
            }

            CacheMisses++;
            return false;
        }

        /// <summary>
        /// Gets an item from the cache.
        /// </summary>
        public static bool TryGet<T>(IWikiCacheKey cacheKey, [NotNullWhen(true)] out T? result)
        {
            var cached = MemCache.Get(cacheKey.Key);

            CacheGets++;
            if (cached == null)
            {
                result = default;
                CacheMisses++;
                return false;
            }

            result = (T)cached;

            CacheHits++;

            return true;
        }

        /// <summary>
        /// Tries to get a value from cache, if it does not exist then a delegate is called to get the value and it is then cached.
        /// </summary>
        public static T? AddOrGet<T>(IWikiCacheKey cacheKey, GetValueDelegate<T?> getValueDelegate, int? seconds = null)
        {
            if (_memCache == null)
            {
                return getValueDelegate();
            }

            if (TryGet<T>(cacheKey, out var result))
            {
                return result;
            }

            result = getValueDelegate();

            if (result != null)
            {
                var policy = new CacheItemPolicy()
                {
                    AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(seconds ?? DefaultCacheSeconds)
                };
                MemCache.Add(cacheKey.Key, result, policy);
            }

            return result;
        }

        /// <summary>
        /// Adds an item to the cache. If the item is already in the cache, this will reset its expiration.
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="value"></param>
        /// <param name="seconds"></param>
        public static void Put(IWikiCacheKey cacheKey, object value, int? seconds = null)
        {
            CachePuts++;

            seconds ??= DefaultCacheSeconds;
            if (seconds <= 0)
            {
                return;
            }

            var policy = new CacheItemPolicy()
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(seconds ?? DefaultCacheSeconds)
            };
            MemCache.Add(cacheKey.Key, value, policy);
        }

        public static void Put(IWikiCacheKey cacheKey, object value, CacheItemPolicy policy)
        {
            CachePuts++;
            MemCache.Add(cacheKey.Key, value, policy);
        }

        public static void Remove(IWikiCacheKey cacheKey)
            => MemCache.Remove(cacheKey.Key);

        /// <summary>
        /// Removes all entries from the cache.
        /// </summary>
        public static void Clear()
        {
            if (_memCache == null)
            {
                return;
            }

            var items = MemCache.ToList();
            foreach (var a in items)
            {
                MemCache.Remove(a.Key);
            }
        }

        /// <summary>
        /// Removes cache entries that begin with the given cache key.
        /// </summary>
        /// <param name="category"></param>
        public static void ClearCategory(WikiCacheKey cacheKey)
        {
            var keys = new List<string>();

            foreach (var item in MemCache)
            {
                if (item.Key.StartsWith(cacheKey.Key))
                {
                    keys.Add(item.Key);
                }
            }

            keys.ForEach(o => MemCache.Remove(o));
        }

        /// <summary>
        /// Removes cache entries in a given category.
        /// </summary>
        /// <param name="category"></param>
        public static void ClearCategory(Category category)
        {
            var cacheKey = WikiCacheKey.Build(category);

            var keys = new List<string>();

            foreach (var item in MemCache)
            {
                if (item.Key.StartsWith(cacheKey.Key))
                {
                    keys.Add(item.Key);
                }
            }

            keys.ForEach(o => MemCache.Remove(o));
        }
    }
}
