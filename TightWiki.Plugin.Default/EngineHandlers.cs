using Microsoft.Extensions.Logging;
using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Attributes.Handlers;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Models;

namespace TightWiki.Plugin.Default
{
    /// <summary>
    /// Handler functions for various wiki operations.
    /// </summary>
    [TwPlugin("Exception and Completion Handlers", "Built-in completion and exception handlers.", 1)]
    public class EngineHandlers
    {
        /// <summary>
        /// Handles wiki completion events. Is called when the wiki processing completes for a given page.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        [TwCompletionPluginHandler("Default completion handler", "Handles compile completion events.", 1)]
        public async Task<TwPluginResult> HandleCompletion(ITwEngineState state)
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

        /// <summary>
        /// Called when an exception is thrown by the wiki engine.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="level">Log level of the exception.</param>
        /// <param name="text">Text that accompanies the exception.</param>
        /// <param name="ex">Optional exception, in the case that this was an actual exception.</param>
        [TwExceptionPluginHandler("Default exception handler", "Handles exceptions thrown by the engine.", 1)]
        public async Task<TwPluginResult> HandleException(ITwEngineState state, LogLevel level, string text, Exception? ex = null)
        {
            if (ex != null)
            {
                state.Logger.Log(level, text, ex);
            }
            else
            {
                state.Logger.Log(level, text);
            }

            return new TwPluginResult();
        }
    }
}
