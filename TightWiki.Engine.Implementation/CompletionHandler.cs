using TightWiki.Configuration;
using TightWiki.Engine.Library.Interfaces;
using TightWiki.Repository;

namespace TightWiki.Engine.Implementation
{
    public class CompletionHandler : ICompletionHandler
    {
        public void Complete(ITightEngineState state)
        {
            if (GlobalConfiguration.RecordCompilationMetrics)
            {
                StatisticsRepository.InsertCompilationStatistics(state.Page.Id,
                    state.ProcessingTime.TotalMilliseconds,
                    state.MatchCount,
                    state.ErrorCount,
                    state.OutgoingLinks.Count,
                    state.Tags.Count,
                    state.BodyResult.Length,
                    state.Page.Body.Length);
            }
        }
    }
}
