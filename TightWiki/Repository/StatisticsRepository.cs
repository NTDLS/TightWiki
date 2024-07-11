using TightWiki.Models.DataModels;

namespace TightWiki.Repository
{
    public static class StatisticsRepository
    {
        public static void InsertPageStatistics(int pageId,
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

            ManagedDataStorage.Statistics.Execute("InsertPageStatistics.sql", param);
        }

        public static List<PageFileAttachmentInfo> GetPageFileAttachmentRevisionsByPageAndFileNavigationPaged(int pageNumber, int? pageSize = null)
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

                var result = o.Query<PageFileAttachmentInfo>(
                    "GetPageStatisticsPaged.sql", param).ToList();

                return result;
            });
        }
    }
}
