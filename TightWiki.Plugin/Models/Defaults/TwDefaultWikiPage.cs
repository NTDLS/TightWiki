namespace TightWiki.Plugin.Models.Defaults
{
    /// <summary>
    /// Represents a default wiki page used to seed the database with built-in content on first run,
    /// such as help pages, include pages, and other pages required for the wiki's initial operation.
    /// </summary>
    public class TwDefaultWikiPage
    {
        /// <summary>
        /// The full name of the page, including namespace prefix if applicable.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The namespace this page belongs to, or an empty string if it has no namespace.
        /// </summary>
        public string Namespace { get; set; } = string.Empty;

        /// <summary>
        /// The URL-safe navigation path used to locate this page.
        /// </summary>
        public string Navigation { get; set; } = string.Empty;

        /// <summary>
        /// A short description of the page content, used in search results and meta tags.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// The revision number of this default page.
        /// </summary>
        public int Revision { get; set; }

        /// <summary>
        /// A hash of the page content used to detect whether the default content has changed.
        /// </summary>
        public int DataHash { get; set; }

        /// <summary>
        /// The raw wiki markup body of this default page.
        /// </summary>
        public string Body { get; set; } = string.Empty;
    }
}