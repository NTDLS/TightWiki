using Microsoft.Extensions.Caching.Memory;
using System.Reflection;
using System.Text;

namespace TightWiki.Library
{
    /// <summary>
    /// Used to read EmbeddedResources from assemblies.
    /// </summary>
    public static class TwEmbeddedResourceReader
    {
        private static readonly MemoryCache _cache = new(new MemoryCacheOptions());

        /// <summary>
        /// Loads the text content of an embedded resource from the specified resource path.
        /// </summary>
        /// <remarks>The method searches all loaded assemblies for the resource and uses an internal cache
        /// to improve performance on repeated calls. If the resource is not found, an exception is thrown rather than
        /// returning null.</remarks>
        /// <param name="resourcePath">The path to the embedded resource to load. The path is case-insensitive and should use dot-separated
        /// namespace notation or file path format.</param>
        /// <returns>A string containing the text content of the embedded resource if found.</returns>
        /// <exception cref="Exception">Thrown if the embedded resource cannot be found at the specified path.</exception>
        public static string LoadText(string resourcePath)
        {
            var bytes = LoadBytes(resourcePath);
            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// Retrieves the embedded resource as a byte array from the specified resource path.
        /// </summary>
        /// <remarks>The method searches all loaded assemblies for the resource and uses an internal cache
        /// to improve performance on repeated calls. The resource path is normalized for lookup.</remarks>
        /// <param name="resourcePath">The path to the embedded resource to load. The path is case-insensitive and may use either '/' or '\' as
        /// separators.</param>
        /// <returns>A byte array containing the contents of the embedded resource. Returns the cached value if available.</returns>
        /// <exception cref="Exception">Thrown if the embedded resource cannot be found at the specified path.</exception>
        public static byte[] LoadBytes(string resourcePath)
        {
            string cacheKey = $":{resourcePath.ToLowerInvariant()}".Replace('.', ':').Replace('\\', ':').Replace('/', ':');

            if (_cache.Get(cacheKey) is byte[] cachedResourceBytes)
            {
                return cachedResourceBytes;
            }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                var resourceBytes = SearchAssembly(assembly, cacheKey, resourcePath);
                if (resourceBytes != null)
                {
                    return resourceBytes;
                }
            }

            throw new Exception($"The embedded resource could not be found after enumeration: '{resourcePath}'");
        }

        /// <summary>
        /// Searches the given assembly for a file.
        /// </summary>
        private static byte[]? SearchAssembly(Assembly assembly, string resourceCacheKey, string resourceName)
        {
            var assemblyCacheKey = $"EmbeddedResources:SearchAssembly:{assembly.FullName}";

            var allResourceNames = _cache.Get(assemblyCacheKey) as List<string>;
            if (allResourceNames == null)
            {
                allResourceNames = assembly.GetManifestResourceNames().Select(o => $":{o}".Replace('.', ':')).ToList();
                _cache.Set(assemblyCacheKey, allResourceNames, new MemoryCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromHours(1)
                });
            }

            if (allResourceNames.Count > 0)
            {
                var resource = allResourceNames.Where(o => o.EndsWith(resourceCacheKey, StringComparison.InvariantCultureIgnoreCase)).ToList();
                if (resource.Count > 1)
                {
                    throw new Exception($"Ambiguous resource name: [{resourceName}].");
                }
                else if (resource.Count == 0)
                {
                    return null;
                }

                using var stream = assembly.GetManifestResourceStream(resource.Single().Replace(':', '.').Trim(['.']))
                    ?? throw new InvalidOperationException($"Resource not found: [{resourceName}].");

                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                byte[] bytes = ms.ToArray();

                _cache.Set(resourceCacheKey, bytes, new MemoryCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromHours(1)
                });

                return bytes;
            }

            return null;
        }
    }
}
