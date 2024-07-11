using TightWiki.Models.DataModels;

namespace TightWiki.Repository
{
    public static class StatisticsRepository
    {
        public static void InsertCompilationStatistics(int pageId,
            double wikifyTimeMs, int matchCount, int errorCount, int outgoingLinkCount,
            int tagCount, int processedBodySize, int bodySize)
        {
            var param = new
            {
                PageId = pageId,
                WikifyTimeMs = wikifyTimeMs,
                MatchCount = matchCount,
                ErrorCount = errorCount,
                OutgoingLinkCount = outgoingLinkCount,
                TagCount = tagCount,
                ProcessedBodySize = processedBodySize,
                BodySize = bodySize
            };

            ManagedDataStorage.Statistics.Execute("InsertCompilationStatistics.sql", param);
        }

        public static void PurgeCompilationStatistics()
        {
            ManagedDataStorage.Statistics.Execute("PurgeCompilationStatistics.sql");
        }

        public static List<PageCompilationStatistics> GetCompilationStatisticsPaged(int pageNumber, int? pageSize = null)
        {
            pageSize ??= ConfigurationRepository.Get<int>("Customization", "Pagination Size");

            var param = new
            {
                PageSize = pageSize,
                PageNumber = pageNumber
            };

            return ManagedDataStorage.Statistics.Ephemeral(o =>
            {
                using var users_db = o.Attach("pages.db", "pages_db");

                var result = o.Query<PageCompilationStatistics>(
                    "GetCompilationStatisticsPaged.sql", param).ToList();

                return result;
            });
        }
    }
}
