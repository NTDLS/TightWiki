namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents a configuration group along with all of its associated configuration entries,
    /// used to organize and present related configuration settings together.
    /// </summary>
    public class TwConfigurationNest
    {
        /// <summary>
        /// The unique identifier of this configuration group.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The name of this configuration group.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// A human-readable description of what this configuration group controls.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// The list of configuration entries belonging to this group.
        /// </summary>
        public List<TwConfigurationEntry> Entries = new();
    }
}