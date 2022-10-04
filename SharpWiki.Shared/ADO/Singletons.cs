using System.Collections.Generic;
using System.Configuration;
using System.Runtime.Caching;

namespace SharpWiki.Shared.ADO
{
    public static class Singletons
    {
        public static MemoryCache Cache { get; set; } = new MemoryCache("RepositoryBase");
        public static int CommandTimeout { get; private set; } = 60;
        private static string connectionString = null;

        public static string ConnectionString
        {
            get
            {
                if (connectionString == null)
                {
                    connectionString = ConfigurationManager.ConnectionStrings["SharpWikiADO"].ConnectionString;
                }
                return connectionString;
            }
        }

        public static T GetCacheItem<T>(string key)
        {
            return (T)Cache.Get(key);
        }

        public static void PutCacheItem(string key, object value)
        {
            if (value == null)
            {
                return;
            }

            var policy = new CacheItemPolicy()
            {
                AbsoluteExpiration = System.DateTimeOffset.Now.AddMinutes(1)
            };
            Cache.Add(key, value, policy);
        }

        public static void ClearCacheItems(string startOfKey)
        {
            var keys = new List<string>();

            foreach (var item in Singletons.Cache)
            {
                if (item.Key.StartsWith(startOfKey))
                {
                    keys.Add(item.Key);
                }
            }

            foreach (var key in keys)
            {
                Cache.Remove(key);
            }
        }
    }
}
