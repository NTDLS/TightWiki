using TightWiki.Plugin.Models;

namespace TightWiki.Library
{
    /// <summary>
    /// The outcome of saving a page. When <see cref="Error"/> is not <see cref="PageSaveError.None"/> nothing was saved.
    /// </summary>
    public class PageSaveResult
    {
        public PageSaveError Error { get; set; }

        /// <summary>The saved page, set when the page was saved.</summary>
        public TwPage? Page { get; set; }

        /// <summary>The navigation the page had before it was renamed, empty when the page was not renamed.</summary>
        public string OriginalNavigation { get; set; } = string.Empty;
    }
}
