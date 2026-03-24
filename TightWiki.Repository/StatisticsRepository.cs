using Microsoft.AspNetCore.Mvc.RazorPages;
using TightWiki.Models;
using TightWiki.Models.DataModels;

namespace TightWiki.Repository
{
    public static class StatisticsRepository
    {
        public static void MergeCompilationStatistics(int pageId,
            double wikifyTimeMs, int matchCount, int errorCount, int outgoingLinkCount,
            int tagCount, int processedBodySize, int bodySize)
        {
            var param = new
            {
                PageId = pageId,
                LastCompileDateTime = DateTime.UtcNow,
                LastWikifyTimeMs = wikifyTimeMs,
                LastMatchCount = matchCount,
                LastErrorCount = errorCount,
                LastOutgoingLinkCount = outgoingLinkCount,
                LastTagCount = tagCount,
                LastProcessedBodySize = processedBodySize,
                LastBodySize = bodySize
            };

            ManagedDataStorage.Statistics.Execute("MergeCompilationStatistics.sql", param);
        }

        public static void PurgeCompilationStatistics()
            => ManagedDataStorage.Statistics.Execute("PurgeCompilationStatistics.sql");

        public static List<PageCompilationStatistics> GetCompilationStatisticsPaged(
            int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
        {
            pageSize ??= GlobalConfiguration.PaginationSize;

            var param = new
            {
                PageSize = pageSize,
                PageNumber = pageNumber
            };

            return ManagedDataStorage.Statistics.Ephemeral(o =>
            {
                using var users_db = o.Attach("pages.db", "pages_db");

                var query = RepositoryHelper.TransposeOrderby("GetCompilationStatisticsPaged.sql", orderBy, orderByDirection);
                return o.Query<PageCompilationStatistics>(query, param).ToList();
            });
        }
    }
}
