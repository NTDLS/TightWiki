using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Attributes.Handlers;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;

namespace TightWiki.Plugin.Default.Handlers
{
    /// <summary>
    /// Handles wiki completion events.
    /// </summary>
    [TwPlugin("Default completion handler", "Handles wiki completion events.", 1000)]
    public class CompletionHandler
    {
        /// <summary>
        /// Handles wiki completion events. Is called when the wiki processing completes for a given page.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        [TwCompletionPluginHandler("Default completion handler", "Handles wiki completion events.", 1000)]
        public async Task<TwPluginResult> Handle(ITwEngineState state)
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

            return new TwPluginResult();
        }
    }
}
