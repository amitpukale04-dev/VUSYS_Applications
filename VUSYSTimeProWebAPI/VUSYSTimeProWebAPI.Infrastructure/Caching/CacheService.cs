using Microsoft.Extensions.Caching.Memory;
using VUSYSTimeProWebAPI.Application.Abstractions.Caching;

namespace VUSYSTimeProWebAPI.Infrastructure.Caching
{
    public sealed class CacheService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly List<string> _cacheKeys;

        /// <summary>
        /// Cache Service Constructor
        /// </summary>
        /// <param name="memoryCache"></param>
        public CacheService(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
            _cacheKeys = new List<string>();
        }

        /// <summary>
        /// This Method Is Used to Set an Object into Cache.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="item"></param>
        /// <param name="absoluteExpiration"></param>
        /// <param name="slidingExpiration"></param>
        public void Set<T>(string cacheKey, T item, TimeSpan? absoluteExpiration = null, TimeSpan? slidingExpiration = null)
        {
            var cacheOptions = new MemoryCacheEntryOptions();

            if (absoluteExpiration.HasValue)
                cacheOptions.SetAbsoluteExpiration(absoluteExpiration.Value);

            if (slidingExpiration.HasValue)
                cacheOptions.SetSlidingExpiration(slidingExpiration.Value);

            _memoryCache.Set(cacheKey, item, cacheOptions);

            // Add the key to the list
            _cacheKeys.Add(cacheKey);
        }


        /// <summary>
        /// This Method Is Used to Update an Object into Cache.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="item"></param>
        /// <param name="absoluteExpiration"></param>
        /// <param name="slidingExpiration"></param>
        public void Update<T>(string cacheKey, T item, TimeSpan? absoluteExpiration = null, TimeSpan? slidingExpiration = null)
        {
            var cacheOptions = new MemoryCacheEntryOptions();

            if (absoluteExpiration.HasValue)
                cacheOptions.SetAbsoluteExpiration(absoluteExpiration.Value);

            if (slidingExpiration.HasValue)
                cacheOptions.SetSlidingExpiration(slidingExpiration.Value);

            if (!_memoryCache.TryGetValue(cacheKey, out var cachedValue) || !Equals(cachedValue, item))
            {
                // Update the cache only if the value has changed
                _memoryCache.Set(cacheKey, item, cacheOptions);
            }
            else
            {
                // Add the key to the list
                _cacheKeys.Add(cacheKey);
            }

            //_memoryCache.Set(cacheKey, item, cacheOptions);
        }


        /// <summary>
        /// This Method Is Used to Get an Object from Cache.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <returns></returns>
        public T Get<T>(string cacheKey)
        {
            if (_memoryCache.TryGetValue(cacheKey, out T cachedItem))
            {
                return cachedItem;
            }

            return default; // Return default value if not found
        }

        /// <summary>
        /// This Method Is Used to Remove an Object into Cache.
        /// </summary>
        /// <param name="cacheKey"></param>
        public void Remove(string cacheKey)
        {
            _memoryCache.Remove(cacheKey);
            _cacheKeys.Remove(cacheKey);
        }

        /// <summary>
        /// This Method Is Used to Get All Cache Keys.
        /// </summary>
        /// <returns></returns>
        public List<string> GetAllCacheKeys()
        {
            return new List<string>(_cacheKeys);  // Return a copy of the list of keys
        }
    }
}
