using TightWiki.DataStorage;

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

            ManagedDataStorage.Statistics.Execute("InsertPageStatistics", param);
        }
    }
}
