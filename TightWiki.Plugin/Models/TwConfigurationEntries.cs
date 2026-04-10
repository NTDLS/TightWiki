namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents a collection of configuration entries, providing typed accessor methods
    /// for retrieving configuration values by name within a configuration group.
    /// </summary>
    public class TwConfigurationEntries
    {
        /// <summary>
        /// The list of configuration entries in this collection.
        /// </summary>
        public List<TwConfigurationEntry> Collection { get; set; }

        /// <summary>
        /// Initializes a new empty configuration entries collection.
        /// </summary>
        public TwConfigurationEntries()
        {
            Collection = new();
        }

        /// <summary>
        /// Initializes a new configuration entries collection populated with the specified entries.
        /// </summary>
        /// <param name="entries">The list of configuration entries to initialize the collection with.</param>
        public TwConfigurationEntries(List<TwConfigurationEntry> entries)
        {
            Collection = new List<TwConfigurationEntry>(entries);
        }

        /// <summary>
        /// Returns the value of the configuration entry with the specified name converted to the specified type,
        /// or null if the entry is not found or conversion fails.
        /// </summary>
        /// <param name="name">The name of the configuration entry to retrieve.</param>
        public T? Value<T>(string name)
        {
            var value = Collection.Where(o => o.Name == name).FirstOrDefault();
            if (value == null)
            {
                return default;
            }
            return value.As<T>();
        }

        /// <summary>
        /// Returns the value of the configuration entry with the specified name converted to the specified type,
        /// or the provided default value if the entry is not found or conversion fails.
        /// </summary>
        /// <param name="name">The name of the configuration entry to retrieve.</param>
        /// <param name="defaultValue">The value to return if the entry is not found or conversion fails.</param>
        public T Value<T>(string name, T defaultValue)
        {
            var value = Collection.Where(o => o.Name == name).FirstOrDefault();
            if (value == null)
            {
                return defaultValue;
            }
            return value.As<T>() ?? defaultValue;
        }
    }
}
