using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki.Repository
{
    public class StatisticsRepository(ITwConfigurationRepository configurationRepository)
        : IStatisticsRepository
    {
        public async Task IncrementPageViewCount(int pageId)
        {
            var param = new
            {
                PageId = pageId,
                LastCompileDateTime = DateTime.UtcNow //Because the file is not nullable.
            };
            await ManagedDataStorage.Statistics.ExecuteAsync("IncrementPageViewCount.sql", param);
        }

        public async Task MergePageCompilationStatistics(int pageId,
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

            await ManagedDataStorage.Statistics.ExecuteAsync("MergePageCompilationStatistics.sql", param);
        }

        public async Task<int> GetPageTotalViewCount(int pageId)
            => await ManagedDataStorage.Statistics.ExecuteScalarAsync<int>("GetPageTotalViewCount.sql", new { PageId = pageId });

        public async Task PurgePageStatistics()
            => await ManagedDataStorage.Statistics.ExecuteAsync("PurgePageStatistics.sql");

        public async Task<List<TwPageStatistics>> GetPageStatisticsPaged(
            int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
        {
            pageSize ??= await configurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

            var param = new
            {
                PageSize = pageSize,
                PageNumber = pageNumber
            };

            return await ManagedDataStorage.Statistics.EphemeralAsync(async o =>
            {
                using var users_db = o.Attach("pages.db", "pages_db");

                var query = RepositoryHelpers.TransposeOrderby("GetPageStatisticsPaged.sql", orderBy, orderByDirection);
                return await o.QueryAsync<TwPageStatistics>(query, param);
            });
        }
    }
}
