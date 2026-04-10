namespace TightWiki.Plugin.Models.Defaults
{
    /// <summary>
    /// Represents a canned configuration entry used to seed the database with default configuration values
    /// on first run or when resetting configuration to defaults.
    /// </summary>
    public class TwDefaultConfiguration
    {
        /// <summary>
        /// The name of the configuration group this entry belongs to.
        /// </summary>
        public string ConfigurationGroupName { get; set; } = string.Empty;

        /// <summary>
        /// The name of the configuration entry to seed.
        /// </summary>
        public string ConfigurationEntryName { get; set; } = string.Empty;

        /// <summary>
        /// The default value to seed for this configuration entry.
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// The identifier of the data type for this configuration entry.
        /// </summary>
        public int DataTypeId { get; set; }

        /// <summary>
        /// A human-readable description of the configuration group this entry belongs to.
        /// </summary>
        public string ConfigurationGroupDescription { get; set; } = string.Empty;

        /// <summary>
        /// A human-readable description of what this configuration entry controls.
        /// </summary>
        public string ConfigurationEntryDescription { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether the value of this configuration entry should be stored encrypted.
        /// </summary>
        public bool IsEncrypted { get; set; }

        /// <summary>
        /// Indicates whether this configuration entry must have a value set.
        /// </summary>
        public bool IsRequired { get; set; }
    }
}