using Microsoft.Extensions.Configuration;
using NTDLS.SqliteDapperWrapper;
using TightWiki.Library.Extensions;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;
using TightWiki.Repository.Helpers;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki.Repository
{
    public class StatisticsRepository
        : ITwStatisticsRepository
    {
        readonly private ITwConfigurationRepository _configurationRepository;
        public SqliteManagedFactory StatisticsFactory { get; private set; }

        public StatisticsRepository(IConfiguration configuration, ITwConfigurationRepository configurationRepository)
        {
            _configurationRepository = configurationRepository;

            var configDatabaseFile = configurationRepository.ConfigFactory.Ephemeral(o => o.NativeConnection.DataSource);

            StatisticsFactory = new SqliteManagedFactory(configuration.GetDatabaseConnectionString("StatisticsConnection", "statistics.db", configDatabaseFile));
        }

        public async Task IncrementPageViewCount(int pageId)
        {
            var param = new
            {
                PageId = pageId,
                LastCompileDateTime = DateTime.UtcNow //Because the file is not nullable.
            };
            await StatisticsFactory.ExecuteAsync("IncrementPageViewCount.sql", param);
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

            await StatisticsFactory.ExecuteAsync("MergePageCompilationStatistics.sql", param);
        }

        public async Task<int> GetPageTotalViewCount(int pageId)
            => await StatisticsFactory.ExecuteScalarAsync<int>("GetPageTotalViewCount.sql", new { PageId = pageId });

        public async Task PurgePageStatistics()
            => await StatisticsFactory.ExecuteAsync("PurgePageStatistics.sql");

        public async Task<List<TwPageStatistics>> GetPageStatisticsPaged(
            int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
        {
            pageSize ??= await _configurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

            var param = new
            {
                PageSize = pageSize,
                PageNumber = pageNumber
            };

            return await StatisticsFactory.EphemeralAsync(async o =>
            {
                using var users_db = o.Attach("pages.db", "pages_db");

                var query = RepositoryHelpers.TransposeOrderby("GetPageStatisticsPaged.sql", orderBy, orderByDirection);
                return await o.QueryAsync<TwPageStatistics>(query, param);
            });
        }

        public async Task<int> DeletePageStatisticsByPageId(int pageId)
            => await StatisticsFactory.ExecuteScalarAsync<int>("DeletePageStatisticsByPageId.sql", new { PageId = pageId });
    }
}
