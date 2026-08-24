namespace TightWiki.Data.EfCore.Entities.Config
{
    /// <summary>
    /// A single named configuration value belonging to a <see cref="ConfigurationGroup"/>
    /// (Config.ConfigurationEntry).
    /// </summary>
    public class ConfigurationEntry
    {
        /// <summary>
        /// The unique identifier for this configuration entry.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The identifier of the <see cref="ConfigurationGroup"/> this entry belongs to.
        /// </summary>
        public int ConfigurationGroupId { get; set; }

        /// <summary>
        /// The name of this configuration entry. Unique together with <see cref="ConfigurationGroupId"/>.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The raw string value of this configuration entry. May be encrypted, see <see cref="IsEncrypted"/>.
        /// </summary>
        public string? Value { get; set; }

        /// <summary>
        /// The identifier of the <see cref="DataType"/> this entry's value should be interpreted as.
        /// </summary>
        public int DataTypeId { get; set; }

        /// <summary>
        /// A human-readable description of what this configuration entry controls.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Indicates whether <see cref="Value"/> is stored encrypted.
        /// </summary>
        public bool IsEncrypted { get; set; }

        /// <summary>
        /// Indicates whether this configuration entry is required to have a value.
        /// </summary>
        public bool IsRequired { get; set; }
    }
}
