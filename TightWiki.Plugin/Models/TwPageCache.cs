namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents a cached pre-processed wiki page result, storing the rendered body
    /// and any custom title set during processing to avoid redundant re-rendering.
    /// </summary>
    public class TwPageCache
    {
        /// <summary>
        /// The custom page title set by a call to @@Title("...") during page processing,
        /// or null if no custom title was specified.
        /// </summary>
        public string? PageTitle { get; set; }

        /// <summary>
        /// The fully rendered HTML body of the wiki page.
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// Initializes a new page cache entry with the specified rendered body.
        /// </summary>
        /// <param name="body">The fully rendered HTML body of the wiki page.</param>
        public TwPageCache(string body)
        {
            Body = body;
        }
    }
}