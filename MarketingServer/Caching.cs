using System;
using System.Runtime.Caching;

namespace MarketingServer
{
    public class Caching
    {
        private static readonly ObjectCache Cache = MemoryCache.Default;

        public static T Get<T>(string key) where T : class
        {
            try
            {
                return (T)Cache[key];
            }
            catch (Exception)
            {

                return null;
            }
        }

        public static void Add<T>(string key, T item) where T : class
        {
            CacheItemPolicy policy = new CacheItemPolicy();
            policy.SlidingExpiration = TimeSpan.FromMinutes(15);

            Cache.Add(key, item, policy);
        }
    }
}