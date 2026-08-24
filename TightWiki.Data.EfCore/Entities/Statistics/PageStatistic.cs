using TightWiki.Data.EfCore.Entities.Pages;

namespace TightWiki.Data.EfCore.Entities.Statistics
{
    /// <summary>
    /// Compilation/view statistics for a single wiki page (Statistics.PageStatistics). The underlying table was
    /// renamed from "CompilationStatistics" to "PageStatistics" in version 2.31.1; the surviving unique index
    /// still carries the original name.
    /// </summary>
    public class PageStatistic
    {
        /// <summary>
        /// The unique identifier for this statistics row.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The identifier of the page (Pages schema) these statistics belong to. See <see cref="Page"/> for the
        /// cross-schema navigation.
        /// </summary>
        public int PageId { get; set; }

        /// <summary>
        /// The date/time of the most recent markup compilation of this page.
        /// </summary>
        public DateTime LastCompileDateTime { get; set; }

        /// <summary>
        /// The total number of times this page has been compiled.
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

        /// <summary>
        /// The page (cross-schema navigation to Pages.Page, via <see cref="PageId"/>) these statistics belong
        /// to. Required - unlike the *UserId navigations elsewhere in this model, application code deletes this
        /// row (StatisticsRepository.DeletePageStatisticsByPageId) whenever its page is deleted, and every real
        /// query joining the two (e.g. GetPageStatisticsPaged.sql) uses an INNER JOIN, not a LEFT OUTER JOIN.
        /// </summary>
        public Page Page { get; set; } = null!;
    }
}
