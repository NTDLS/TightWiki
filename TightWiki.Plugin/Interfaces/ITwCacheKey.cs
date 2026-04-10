namespace TightWiki.Plugin.Interfaces
{
    /// <summary>
    /// Defines a contract for objects that provide a cache key used to uniquely identify cached items.
    /// </summary>
    /// <remarks>Implement this interface to ensure that an object can be used as a cache key in caching
    /// mechanisms. The value of the key should be unique for each distinct cached item to avoid collisions.</remarks>
    public interface ITwCacheKey
    {
        /// <summary>
        /// Gets or sets the unique identifier associated with this instance.
        /// </summary>
        public string Key { get; set; }
    }
}
