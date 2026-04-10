namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents the content and registration metadata for a loaded TightWiki plugin,
    /// including the runtime type used to instantiate the plugin and its evaluation precedence.
    /// </summary>
    public class TwPluginContent
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
        /// The runtime type of the plugin implementation, used to instantiate the plugin via reflection.
        /// </summary>
        public Type? Type { get; set; }

        /// <summary>
        /// The precedence order of this plugin relative to other plugins. Lower values are evaluated first.
        /// </summary>
        public int Precedence { get; set; }
    }
}