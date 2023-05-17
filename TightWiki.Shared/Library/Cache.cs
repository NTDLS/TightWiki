using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.Caching;

namespace TightWiki.Shared.Library
{
    public class Cache
    {
        const int DefaultCacheSeconds = 5 * 60;
        private static MemoryCache _memcache;
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
                    config.Add("CacheMemoryLimitMegabytes", Global.CacheMemoryLimitMB.ToString());
                    _memcache = new MemoryCache("TightWikiCache", config);
                }
                return _memcache;
            }
        }



        public static T Get<T>(string key)
        {
            CacheGets++;
            var result = (T)Memcache.Get(key);

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

        public static void Put(string key, object value, int seconds = DefaultCacheSeconds)
        {
            CachePuts++;

            if (value == null)
            {
                return;
            }

            var policy = new CacheItemPolicy()
            {
                AbsoluteExpiration = System.DateTimeOffset.Now.AddSeconds(seconds)
            };
            Memcache.Add(key, value, policy);
        }

        public static void Clear()
        {
            var items = Memcache.ToList();
            foreach (var a in items)
            {
                Memcache.Remove(a.Key);
            }
        }

        /// <summary>
        /// Removes cache keys that start with startOfKey.
        /// </summary>
        /// <param name="startOfKey"></param>
        public static void ClearClass(string startOfKey)
        {
            var keys = new List<string>();

            foreach (var item in Memcache)
            {
                if (item.Key.StartsWith(startOfKey))
                {
                    keys.Add(item.Key);
                }
            }

            foreach (var key in keys)
            {
                Memcache.Remove(key);
            }
        }
    }
}
