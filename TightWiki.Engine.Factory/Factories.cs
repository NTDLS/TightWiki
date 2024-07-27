using TightWiki.Configuration;
using TightWiki.Engine;
using TightWiki.Engine.Handlers;
using TightWiki.Engine.Library.Interfaces;
using TightWiki.Library.Interfaces;
using TightWiki.Repository;
using static TightWiki.Engine.Library.Constants;

namespace TightWiki
{
    public static class Factories
    {
        public static IWikifier CreateWikifier(ISessionState? sessionState, IPage page, int? pageRevision = null, WikiMatchType[]? omitMatches = null)
        {
            return new Wikifier(
                new StandardFunctionHandler(),
                new ScopeFunctionHandler(),
                new ProcessingInstructionFunctionHandler(),
                new PostProcessingFunctionHandler(),
                new MarkupHandler(),
                new HeadingHandler(),
                new CommentHandler(),
                new EmojiHandler(),
                new ExternalLinkHandler(),
                new InternalLinkHandler(),
                ExceptionLogger,
                OnCompletion,
                sessionState, page, pageRevision, omitMatches);
        }

        public static void ExceptionLogger(Wikifier wikifier, Exception? ex, string customText)
        {
            if (ex != null)
            {
                ExceptionRepository.InsertException(ex, customText);
            }

            ExceptionRepository.InsertException(customText);
        }

        public static void OnCompletion(Wikifier wikifier)
        {
            if (GlobalConfiguration.RecordCompilationMetrics)
            {
                StatisticsRepository.InsertCompilationStatistics(wikifier.Page.Id,
                    wikifier.ProcessingTime.TotalMilliseconds,
                    wikifier.MatchCount,
                    wikifier.ErrorCount,
                    wikifier.OutgoingLinks.Count,
                    wikifier.Tags.Count,
                    wikifier.ProcessedBody.Length,
                    wikifier.Page.Body.Length);
            }
        }
    }
}
