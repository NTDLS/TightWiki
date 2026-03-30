using TightWiki.Plugin.Models;

namespace TightWiki.Plugin.Interfaces.Repository
{
    public interface IStatisticsRepository
    {
        Task IncrementPageViewCount(int pageId);
        Task MergePageCompilationStatistics(int pageId, double wikifyTimeMs, int matchCount, int errorCount, int outgoingLinkCount, int tagCount, int processedBodySize, int bodySize);
        Task<int> GetPageTotalViewCount(int pageId);
        Task PurgePageStatistics();
        Task<List<TwPageStatistics>> GetPageStatisticsPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null);
    }
}
