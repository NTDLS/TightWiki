using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Caching;
using TightWiki.Plugin.Interfaces;

namespace TightWiki.Library.Caching
{
    public class MemCache
    {
        public delegate T? GetValueDelegate<T>();
        public delegate Task<T?> GetValueDelegateAsync<T>();

        public enum Category
        {
            User,
            Page,
            Security,
            Search,
            Emoji,
            Configuration,
            Session
        }

        public static TimeSpan DefaultCacheExpiration { get; set; }
        private static MemoryCache? _cache;

        //These are updated with Interlocked because the cache is used by many requests at once.
        private static long _cacheSets;
        private static long _cacheGets;
        private static long _cacheHits;
        private static long _cacheMisses;

        public static ulong CacheSets => (ulong)Interlocked.Read(ref _cacheSets);
        public static ulong CacheGets => (ulong)Interlocked.Read(ref _cacheGets);
        public static ulong CacheHits => (ulong)Interlocked.Read(ref _cacheHits);
        public static ulong CacheMisses => (ulong)Interlocked.Read(ref _cacheMisses);
        public static int CacheItemCount => Cache.Count();
        public static double CacheMemoryLimit => Cache.CacheMemoryLimit;

        public static MemoryCache Cache => _cache ?? throw new Exception("Cache has not been initialized.");

        public static void Initialize(int cacheMemoryLimitMB, TimeSpan defaultCacheExpiration)
        {
            DefaultCacheExpiration = defaultCacheExpiration;
            var config = new NameValueCollection
            {
                //config.Add("pollingInterval", "00:05:00");
                //config.Add("physicalMemoryLimitPercentage", "0");
                { "CacheMemoryLimitMegabytes", cacheMemoryLimitMB.ToString() }
            };
            _cache?.Dispose();
            _cache = new MemoryCache("TightWikiCache", config);
        }

        /// <summary>
        /// Gets an item from the cache.
        /// </summary>
        public static T? Get<T>(ITwCacheKey cacheKey)
        {
            Interlocked.Increment(ref _cacheGets);
            var cached = Cache.Get(cacheKey.Key);

            if (cached == null)
            {
                Interlocked.Increment(ref _cacheMisses);
                return default;
            }

            Interlocked.Increment(ref _cacheHits);
            return (T)cached;
        }

        /// <summary>
        /// Determines if the cache contains a given key.
        /// </summary>
        public static bool Contains(ITwCacheKey cacheKey)
        {
            Interlocked.Increment(ref _cacheGets);
            if (Cache.Contains(cacheKey.Key))
            {
                Interlocked.Increment(ref _cacheHits);
                return true;
            }

            Interlocked.Increment(ref _cacheMisses);
            return false;
        }

        /// <summary>
        /// Gets an item from the cache.
        /// </summary>
        public static bool TryGet<T>(ITwCacheKey cacheKey, [NotNullWhen(true)] out T? result)
        {
            var cached = Cache.Get(cacheKey.Key);

            Interlocked.Increment(ref _cacheGets);
            if (cached == null)
            {
                result = default;
                Interlocked.Increment(ref _cacheMisses);
                return false;
            }

            result = (T)cached;

            Interlocked.Increment(ref _cacheHits);

            return true;
        }

        /// <summary>
        /// Tries to get a value from cache, if it does not exist then a delegate is called to get the value and it is then cached.
        /// </summary>
        public static T? AddOrGet<T>(ITwCacheKey cacheKey, bool forceReCache, GetValueDelegate<T?> getValueDelegate, TimeSpan? cacheExpiration = null)
        {
            if (_cache == null)
            {
                return getValueDelegate();
            }

            T? result;

            if (!forceReCache)
            {
                if (TryGet<T>(cacheKey, out result))
                {
                    return result;
                }
            }

            result = getValueDelegate();

            if (result != null)
            {
                cacheExpiration ??= DefaultCacheExpiration;
                if (cacheExpiration.Value.TotalSeconds <= 0)
                {
                    return result;
                }

                var policy = new CacheItemPolicy()
                {
                    AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(cacheExpiration.Value.TotalSeconds)
                };
                Set(cacheKey, result, policy);
            }

            return result;
        }

        /// <summary>
        /// Tries to get a value from cache, if it does not exist then a delegate is called to get the value and it is then cached.
        /// </summary>
        public static async Task<T?> AddOrGetAsync<T>(ITwCacheKey cacheKey, bool forceReCache, GetValueDelegateAsync<T?> getValueDelegate, TimeSpan? cacheExpiration = null)
        {
            if (_cache == null)
            {
                return await getValueDelegate();
            }

            T? result;

            if (!forceReCache)
            {
                if (TryGet<T>(cacheKey, out result))
                {
                    return result;
                }
            }

            result = await getValueDelegate();

            if (result != null)
            {
                cacheExpiration ??= DefaultCacheExpiration;
                if (cacheExpiration.Value.TotalSeconds <= 0)
                {
                    return result;
                }

                var policy = new CacheItemPolicy()
                {
                    AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(cacheExpiration.Value.TotalSeconds)
                };
                Set(cacheKey, result, policy);
            }

            return result;
        }

        /// <summary>
        /// Tries to get a value from cache, if it does not exist then a delegate is called to get the value and it is then cached.
        /// </summary>
        public static T? AddOrGet<T>(ITwCacheKey cacheKey, GetValueDelegate<T?> getValueDelegate, TimeSpan? cacheExpiration = null)
        {
            if (_cache == null)
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
                cacheExpiration ??= DefaultCacheExpiration;
                if (cacheExpiration.Value.TotalSeconds <= 0)
                {
                    return result;
                }

                var policy = new CacheItemPolicy()
                {
                    AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(cacheExpiration.Value.TotalSeconds)
                };
                Set(cacheKey, result, policy);
            }

            return result;
        }

        /// <summary>
        /// Tries to get a value from cache, if it does not exist then a delegate is called to get the value and it is then cached.
        /// </summary>
        public static async Task<T?> AddOrGetAsync<T>(ITwCacheKey cacheKey, GetValueDelegateAsync<T?> getValueDelegate, TimeSpan? cacheExpiration = null)
        {
            if (_cache == null)
            {
                return await getValueDelegate();
            }

            if (TryGet<T>(cacheKey, out var result))
            {
                return result;
            }

            result = await getValueDelegate();

            if (result != null)
            {
                cacheExpiration ??= DefaultCacheExpiration;
                if (cacheExpiration.Value.TotalSeconds <= 0)
                {
                    return result;
                }

                var policy = new CacheItemPolicy()
                {
                    AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(cacheExpiration.Value.TotalSeconds)
                };
                Set(cacheKey, result, policy);
            }

            return result;
        }

        /// <summary>
        /// Adds an item to the cache. If the item is already in the cache, this will reset its expiration.
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="value"></param>
        /// <param name="seconds"></param>
        public static void Set(ITwCacheKey cacheKey, object value, TimeSpan? cacheExpiration = null)
        {
            cacheExpiration ??= DefaultCacheExpiration;
            if (cacheExpiration.Value.TotalSeconds <= 0)
            {
                return;
            }

            var policy = new CacheItemPolicy()
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(cacheExpiration.Value.TotalSeconds)
            };
            Set(cacheKey, value, policy);
        }

        public static void Set(ITwCacheKey cacheKey, object value, CacheItemPolicy policy)
        {
            Interlocked.Increment(ref _cacheSets);
            Cache.Set(cacheKey.Key, value, policy);
        }

        public static void Remove(ITwCacheKey cacheKey)
            => Cache.Remove(cacheKey.Key);

        /// <summary>
        /// Removes all entries from the cache.
        /// </summary>
        public static void Clear()
        {
            if (_cache == null)
            {
                return;
            }

            var items = Cache.ToList();
            foreach (var a in items)
            {
                Cache.Remove(a.Key);
            }
        }

        /// <summary>
        /// Removes cache entries that begin with the given cache key.
        /// </summary>
        /// <param name="category"></param>
        public static void ClearCategory(MemCacheKey cacheKey)
        {
            var keys = new List<string>();

            foreach (var item in Cache)
            {
                if (item.Key.StartsWith(cacheKey.Key))
                {
                    keys.Add(item.Key);
                }
            }

            keys.ForEach(o => Cache.Remove(o));
        }

        /// <summary>
        /// Removes cache entries in a given category.
        /// </summary>
        /// <param name="category"></param>
        public static void ClearCategory(Category category)
        {
            var cacheKey = MemCacheKey.Build(category);

            var keys = new List<string>();

            foreach (var item in Cache)
            {
                if (item.Key.StartsWith(cacheKey.Key))
                {
                    keys.Add(item.Key);
                }
            }

            keys.ForEach(o => Cache.Remove(o));
        }
    }
}
