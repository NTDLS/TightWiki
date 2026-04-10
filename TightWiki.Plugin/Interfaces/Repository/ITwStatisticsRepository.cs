using NTDLS.SqliteDapperWrapper;
using TightWiki.Plugin.Models;

namespace TightWiki.Plugin.Interfaces.Repository
{
    /// <summary>
    /// Data access for page statistics, page view counts, and related data.
    /// </summary>
    public interface ITwStatisticsRepository
    {
        /// <summary>
        /// SQLite factory used to access the statistics database.
        /// </summary>
        SqliteManagedFactory StatisticsFactory { get; }

        /// <summary>
        /// Increments the view count for the specified page by one.
        /// </summary>
        Task IncrementPageViewCount(int pageId);

        /// <summary>
        /// Inserts or updates compilation statistics for the specified page, including render time, match counts, error counts, link counts, tag counts, and body sizes.
        /// </summary>
        Task MergePageCompilationStatistics(int pageId, double wikifyTimeMs, int matchCount, int errorCount, int outgoingLinkCount, int tagCount, int processedBodySize, int bodySize);

        /// <summary>
        /// Returns the total number of views recorded for the specified page.
        /// </summary>
        Task<int> GetPageTotalViewCount(int pageId);

        /// <summary>
        /// Permanently removes all page statistics records.
        /// </summary>
        Task PurgePageStatistics();

        /// <summary>
        /// Returns a paged list of page statistics records, with optional sorting and page size.
        /// </summary>
        Task<List<TwPageStatistics>> GetPageStatisticsPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null);

        /// <summary>
        /// Deletes all statistics records for the specified page. Returns the number of records deleted.
        /// </summary>
        Task<int> DeletePageStatisticsByPageId(int pageId);
    }
}