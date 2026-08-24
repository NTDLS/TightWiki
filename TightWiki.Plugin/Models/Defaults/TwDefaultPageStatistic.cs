namespace TightWiki.Plugin.Models.Defaults
{
    /// <summary>
    /// Represents a default compilation/view statistics row for a default wiki page (Data\statistics.db's
    /// PageStatistics), used to seed Statistics.PageStatistics on first run. Unlike TwDefaultWikiPage, this is not
    /// a "one row per page" table - only pages that actually carry a row in the reference database are
    /// represented here (roughly 99 of the 110 reference wiki pages; the rest have no statistics history at all),
    /// and seeding must reproduce that absence rather than manufacture a row for every page.
    /// </summary>
    public class TwDefaultPageStatistic
    {
        /// <summary>
        /// The primary key of the page these statistics belong to, in the reference SQLite database
        /// (Data\pages.db's Page.Id). Safe to use directly as Statistics.PageStatistics.PageId on the EF providers
        /// because Page.Id is now carried 1:1 from the reference database (see TwDefaultWikiPage.Id) - Navigation
        /// below is carried alongside purely as a portable, human-readable fallback/sanity-check key, the same way
        /// TwDefaultPageFileAttachment/TwDefaultFeatureTemplate join back to a page by name/navigation rather than
        /// by Id alone.
        /// </summary>
        public int PageId { get; set; }

        /// <summary>
        /// The URL-safe navigation path of the page these statistics belong to (Data\pages.db's Page.Navigation).
        /// </summary>
        public string Navigation { get; set; } = string.Empty;

        /// <summary>
        /// The date/time of the most recent markup compilation of this page, as recorded in the reference database.
        /// </summary>
        public DateTime LastCompileDateTime { get; set; }

        /// <summary>
        /// The total number of times this page has been compiled, as recorded in the reference database.
        /// </summary>
        public int TotalCompilationCount { get; set; }

        /// <summary>
        /// The duration, in milliseconds, of the most recent compilation.
        /// </summary>
        public double? LastWikifyTimeMs { get; set; }

        /// <summary>
        /// The cumulative duration, in milliseconds, of all compilations.
        /// </summary>
        public double? TotalWikifyTimeMs { get; set; }

        /// <summary>
        /// The number of markup matches found during the most recent compilation.
        /// </summary>
        public int? LastMatchCount { get; set; }

        /// <summary>
        /// The number of markup errors found during the most recent compilation.
        /// </summary>
        public int? LastErrorCount { get; set; }

        /// <summary>
        /// The number of outgoing links found during the most recent compilation.
        /// </summary>
        public int? LastOutgoingLinkCount { get; set; }

        /// <summary>
        /// The number of tags found during the most recent compilation.
        /// </summary>
        public int? LastTagCount { get; set; }

        /// <summary>
        /// The size, in bytes, of the processed (compiled) page body during the most recent compilation.
        /// </summary>
        public int? LastProcessedBodySize { get; set; }

        /// <summary>
        /// The size, in bytes, of the raw page body during the most recent compilation.
        /// </summary>
        public int? LastBodySize { get; set; }

        /// <summary>
        /// The total number of times this page has been viewed.
        /// </summary>
        public int TotalViewCount { get; set; }
    }
}
