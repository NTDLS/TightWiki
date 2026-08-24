namespace TightWiki.Data.EfCore.Entities.Config
{
    /// <summary>
    /// A named grouping of <see cref="ConfigurationEntry"/> rows (Config.ConfigurationGroup).
    /// </summary>
    public class ConfigurationGroup
    {
        /// <summary>
        /// The unique identifier for this configuration group.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The unique name of this configuration group.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// A human-readable description of this configuration group.
        /// </summary>
        public string? Description { get; set; }
    }
}
