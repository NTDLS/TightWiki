namespace TightWiki.Plugin.Models.Defaults
{
    /// <summary>
    /// Represents a default feature template that describes a function or instruction
    /// a user can add to a wiki page, including its name, type, description, and example markup.
    /// These are seeded into the database to provide users with discoverable, ready-to-use page features.
    /// </summary>
    public partial class TwDefaultFeatureTemplate
    {
        /// <summary>
        /// The display name of this feature template.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The type of feature this template represents, such as a function or processing instruction.
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// The name of the wiki page associated with this feature template, if applicable.
        /// </summary>
        public string PageName { get; set; } = string.Empty;

        /// <summary>
        /// A human-readable description of what this feature template does.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// The example wiki markup text demonstrating how to use this feature on a page.
        /// </summary>
        public string TemplateText { get; set; } = string.Empty;
    }
}