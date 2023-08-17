using System.Runtime.Caching;

namespace ConsoleApp1
{
    public class CacheService
    {
        private static MemoryCache cache = new MemoryCache("BotCache");

        public void AddToCache(string key, object data, DateTimeOffset absExpiration)
        {
            cache.Add(key, data, absExpiration);
        }

        public object GetFromCache(string key)
        {
            try
            {
                return cache.Get(key);
            }
            catch
            {
                return null;
            }
        }
    }

}
