namespace VUSYSTimeProWebAPI.Application.Abstractions.Caching
{
    public interface ICacheService
    {
        /// <summary>
        /// This Method Is Used to Set an Object into Cache.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="item"></param>
        /// <param name="absoluteExpiration"></param>
        /// <param name="slidingExpiration"></param>
        void Set<T>(string cacheKey, T item, TimeSpan? absoluteExpiration = null, TimeSpan? slidingExpiration = null);

        /// <summary>
        /// This Method Is Used to Update an Object into Cache.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="item"></param>
        /// <param name="absoluteExpiration"></param>
        /// <param name="slidingExpiration"></param>
        void Update<T>(string cacheKey, T item, TimeSpan? absoluteExpiration = null, TimeSpan? slidingExpiration = null);

        /// <summary>
        /// This Method Is Used to Get an Object from Cache.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <returns></returns>
        T Get<T>(string cacheKey);

        /// <summary>
        /// This Method Is Used to Remove an Object into Cache.
        /// </summary>
        /// <param name="cacheKey"></param>
        void Remove(string cacheKey);

        /// <summary>
        /// This Method Is Used to Get All Cache Keys.
        /// </summary>
        /// <returns></returns>
        List<string> GetAllCacheKeys();
    }
}