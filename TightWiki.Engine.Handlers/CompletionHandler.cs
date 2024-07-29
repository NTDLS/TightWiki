using TightWiki.Configuration;
using TightWiki.Engine.Library.Interfaces;
using TightWiki.Repository;

namespace TightWiki.Engine.Handlers
{
    public class CompletionHandler : ICompletionHandler
    {
        public void Complete(IWikifier wikifier)
        {
            if (GlobalConfiguration.RecordCompilationMetrics)
            {
                StatisticsRepository.InsertCompilationStatistics(wikifier.Page.Id,
                    wikifier.ProcessingTime.TotalMilliseconds,
                    wikifier.MatchCount,
                    wikifier.ErrorCount,
                    wikifier.OutgoingLinks.Count,
                    wikifier.Tags.Count,
                    wikifier.BodyResult.Length,
                    wikifier.Page.Body.Length);
            }
        }
    }
}
