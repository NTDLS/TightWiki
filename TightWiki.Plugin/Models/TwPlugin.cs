namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents a registered TightWiki plugin, including its identity, description,
    /// and the precedence order used to resolve conflicts between plugins that handle the same content.
    /// </summary>
    public class TwPlugin
    {
        /// <summary>
        /// The unique name of this plugin.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// A human-readable description of what this plugin does.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// The precedence order of this plugin relative to other plugins. Lower values are evaluated first.
        /// </summary>
        public int Precedence { get; set; }
    }
}