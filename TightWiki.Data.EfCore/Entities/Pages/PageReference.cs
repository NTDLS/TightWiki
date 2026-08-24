namespace TightWiki.Data.EfCore.Entities.Pages
{
    /// <summary>
    /// A link from one page to another, discovered during markup compilation (Pages.PageReference). Both foreign
    /// keys target <see cref="Page"/> within this same schema (a self-referencing association).
    /// </summary>
    public class PageReference
    {
        /// <summary>
        /// The identifier of the page that contains the reference (link). Part of the composite primary key
        /// together with <see cref="ReferencesPageNavigation"/>.
        /// </summary>
        public int PageId { get; set; }

        /// <summary>
        /// The case-insensitive name of the referenced page, as written in the source markup.
        /// </summary>
        public string ReferencesPageName { get; set; } = string.Empty;

        /// <summary>
        /// The case-insensitive navigation path of the referenced page. Part of the composite primary key
        /// together with <see cref="PageId"/>.
        /// </summary>
        public string ReferencesPageNavigation { get; set; } = string.Empty;

        /// <summary>
        /// The identifier of the referenced page, if it currently exists.
        /// </summary>
        public int? ReferencesPageId { get; set; }

        /// <summary>
        /// The page that contains the reference (link).
        /// </summary>
        public Page Page { get; set; } = null!;

        /// <summary>
        /// The referenced page, if it currently exists.
        /// </summary>
        public Page? ReferencesPage { get; set; }
    }
}
