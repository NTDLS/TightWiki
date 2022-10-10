using System.Collections.Generic;
using System.Runtime.Caching;

namespace SharpWiki.Shared.Library
{
    public class Cache
    {
        const int DefaultCacheMinutes = 5;

        public static MemoryCache Memcache { get; set; } = new MemoryCache("RepositoryBase");

        public static T Get<T>(string key)
        {
            return (T)Memcache.Get(key);
        }

        public static void Put(string key, object value, int minutes = DefaultCacheMinutes)
        {
            if (value == null)
            {
                return;
            }

            var policy = new CacheItemPolicy()
            {
                AbsoluteExpiration = System.DateTimeOffset.Now.AddMinutes(minutes)
            };
            Memcache.Add(key, value, policy);
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
