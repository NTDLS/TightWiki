namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents a feature template that describes a function or instruction a user can add
    /// to a wiki page, including its markup example and a link to the associated help page.
    /// </summary>
    public partial class TwFeatureTemplate
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
        /// The unique identifier of the wiki page associated with this feature template.
        /// </summary>
        public int PageId { get; set; }

        /// <summary>
        /// A human-readable description of what this feature template does.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// The example wiki markup text demonstrating how to use this feature on a page.
        /// </summary>
        public string TemplateText { get; set; } = string.Empty;

        /// <summary>
        /// The URL-safe navigation path to the help page that documents this feature.
        /// </summary>
        public string HelpPageNavigation { get; set; } = string.Empty;
    }
}