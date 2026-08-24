namespace TightWiki.Data.EfCore.Entities.Pages
{
    /// <summary>
    /// A feature template that describes a function or instruction a user can add to a wiki page, including a
    /// markup example (Pages.FeatureTemplate).
    /// </summary>
    public class FeatureTemplate
    {
        /// <summary>
        /// The display name of this feature template. Part of the composite primary key together with
        /// <see cref="Type"/>.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The type of feature this template represents. Part of the composite primary key together with
        /// <see cref="Name"/>.
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// The identifier of the help page associated with this feature template, if any. Application code sets
        /// this to null when the associated page is deleted; the real schema declares no cascading delete for
        /// this column.
        /// </summary>
        public int? PageId { get; set; }

        /// <summary>
        /// A human-readable description of what this feature template does.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// The example wiki markup text demonstrating how to use this feature.
        /// </summary>
        public string? TemplateText { get; set; }

        /// <summary>
        /// The help page associated with this feature template, if any.
        /// </summary>
        public Page? Page { get; set; }
    }
}
