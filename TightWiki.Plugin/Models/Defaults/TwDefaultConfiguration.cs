namespace TightWiki.Plugin.Models.Defaults
{
    /// <summary>
    /// Represents a canned configuration entry used to seed the database with default configuration values
    /// on first run or when resetting configuration to defaults.
    /// </summary>
    public class TwDefaultConfiguration
    {
        /// <summary>
        /// The unique identifier of the configuration group this entry belongs to (or, when this instance was
        /// returned from <see cref="Interfaces.Repository.ITwDefaultsRepository.GetDefaultConfigurationGroups"/>,
        /// of this group record itself). Preserved from the seed package's Config.ConfigurationGroup.Id so
        /// providers seeding from Seed\tightwiki.seed.zip can insert it verbatim instead of letting the database
        /// generate a new one. Always 0 for the SQLite reference, which never seeds Config.ConfigurationGroup
        /// through this mechanism (see <see cref="Interfaces.Repository.ITwDefaultsRepository"/>'s doc comments).
        /// </summary>
        public int ConfigurationGroupId { get; set; }

        /// <summary>
        /// The unique identifier of this configuration entry. Preserved from the seed package's
        /// Config.ConfigurationEntry.Id, same purpose as <see cref="ConfigurationGroupId"/>. Only meaningful when
        /// this instance was returned from
        /// <see cref="Interfaces.Repository.ITwDefaultsRepository.GetDefaultConfigurations"/> - always 0 when
        /// returned from <see cref="Interfaces.Repository.ITwDefaultsRepository.GetDefaultConfigurationGroups"/>
        /// instead, and always 0 for the SQLite reference (see <see cref="ConfigurationGroupId"/>'s doc comment).
        /// </summary>
        public int ConfigurationEntryId { get; set; }

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