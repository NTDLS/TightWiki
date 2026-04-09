using NTDLS.Helpers;

namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents a single configuration entry within a configuration group,
    /// storing a named value along with its type, description, and encryption state.
    /// </summary>
    public partial class TwConfigurationEntry
    {
        /// <summary>
        /// The unique identifier for this configuration entry.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The unique identifier of the configuration group this entry belongs to.
        /// </summary>
        public int ConfigurationGroupId { get; set; }

        /// <summary>
        /// The name of this configuration entry.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The raw string value of this configuration entry.
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// The identifier of the data type for this configuration entry.
        /// </summary>
        public int DataTypeId { get; set; }

        /// <summary>
        /// A human-readable description of what this configuration entry controls.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether the value of this configuration entry is stored encrypted.
        /// </summary>
        public bool IsEncrypted { get; set; }

        /// <summary>
        /// Converts the raw string value of this entry to the specified type, or returns null if conversion fails.
        /// </summary>
        public T? As<T>()
        {
            return Converters.ConvertTo<T>(Value);
        }

        /// <summary>
        /// Converts the raw string value of this entry to the specified type, or returns the provided default value if the value is null.
        /// </summary>
        public T? As<T>(T defaultValue)
        {
            if (Value == null)
            {
                return defaultValue;
            }

            return Converters.ConvertTo<T>(Value);
        }

        /// <summary>
        /// The name of the data type for this configuration entry.
        /// </summary>
        public string DataType { get; set; } = string.Empty;
    }
}