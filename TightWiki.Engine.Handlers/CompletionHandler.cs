using TightWiki.Configuration;
using TightWiki.Engine.Library.Interfaces;
using TightWiki.Repository;

namespace TightWiki.Engine.Handlers
{
    public class CompletionHandler : ICompletionHandler
    {
        public void Complete(IWikifierSession wikifierSession)
        {
            if (GlobalConfiguration.RecordCompilationMetrics)
            {
                StatisticsRepository.InsertCompilationStatistics(wikifierSession.Page.Id,
                    wikifierSession.ProcessingTime.TotalMilliseconds,
                    wikifierSession.MatchCount,
                    wikifierSession.ErrorCount,
                    wikifierSession.OutgoingLinks.Count,
                    wikifierSession.Tags.Count,
                    wikifierSession.BodyResult.Length,
                    wikifierSession.Page.Body.Length);
            }
        }
    }
}
