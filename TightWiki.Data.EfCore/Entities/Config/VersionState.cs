namespace TightWiki.Data.EfCore.Entities.Config
{
    /// <summary>
    /// Generic name/value state store used to track the currently-applied schema version
    /// (Config.VersionState). Consumed by the versioned schema-upgrade system.
    /// </summary>
    public class VersionState
    {
        /// <summary>
        /// The unique identifier for this state entry.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The unique name of this state entry (e.g. "Version").
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The stored value for this state entry.
        /// </summary>
        public string Value { get; set; } = string.Empty;
    }
}
