using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Handlers;

namespace TightWiki.Engine.Implementation.Handlers
{
    /// <summary>
    /// Handles wiki completion events.
    /// </summary>
    [TwPluginModule("Default completion handler", "Handles wiki completion events.")]
    public class CompletionHandler
        : ITwCompletionHandler
    {
        /// <summary>
        /// Handles wiki completion events. Is called when the wiki processing completes for a given page.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        public async Task Handle(ITwEngineState state)
        {
            if (state.Engine.WikiConfiguration.RecordCompilationMetrics)
            {
                await state.Engine.DatabaseManager.StatisticsRepository.MergePageCompilationStatistics(state.Page.Id,
                    state.ProcessingTime.TotalMilliseconds,
                    state.MatchCount,
                    state.ErrorCount,
                    state.OutgoingLinks.Count,
                    state.Tags.Count,
                    state.HtmlResult.Length,
                    state.Page.Body.Length);
            }
        }
    }
}
