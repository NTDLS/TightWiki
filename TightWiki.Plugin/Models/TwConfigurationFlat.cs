namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents a flattened view of a configuration entry and its parent group,
    /// combining group and entry fields into a single model for simplified querying and display.
    /// </summary>
    public class TwConfigurationFlat
    {
        /// <summary>
        /// The unique identifier of the configuration group.
        /// </summary>
        public int GroupId { get; set; }

        /// <summary>
        /// The name of the configuration group.
        /// </summary>
        public string GroupName { get; set; } = string.Empty;

        /// <summary>
        /// A human-readable description of the configuration group.
        /// </summary>
        public string GroupDescription { get; set; } = string.Empty;

        /// <summary>
        /// The unique identifier of the configuration entry.
        /// </summary>
        public int EntryId { get; set; }

        /// <summary>
        /// The name of the configuration entry.
        /// </summary>
        public string EntryName { get; set; } = string.Empty;

        /// <summary>
        /// The raw string value of the configuration entry.
        /// </summary>
        public string EntryValue { get; set; } = string.Empty;

        /// <summary>
        /// A human-readable description of what the configuration entry controls.
        /// </summary>
        public string EntryDescription { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether the value of this configuration entry is stored encrypted.
        /// </summary>
        public bool IsEncrypted { get; set; }

        /// <summary>
        /// Indicates whether this configuration entry must have a value set.
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// The name of the data type for this configuration entry.
        /// </summary>
        public string DataType { get; set; } = string.Empty;
    }
}