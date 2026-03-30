using Microsoft.Extensions.Logging;
using TightWiki.Plugin.Engine.Function;
using TightWiki.Plugin.Interfaces.Handlers;

namespace TightWiki.Plugin.Interfaces
{
    public interface ITwEngine
    {
        TwConfiguration WikiConfiguration { get; }

        ITwDatabaseManager DatabaseManager { get; }

        ILogger<ITwEngine> Logger { get; }

        List<ITwMarkupHandler> MarkupHandlers { get; }
        List<ITwHeadingHandler> HeadingHandlers { get; }
        List<ITwCommentHandler> CommentHandlers { get; }
        List<ITwEmojiHandler> EmojiHandlers { get; }
        List<ITwExternalLinkHandler> ExternalLinkHandlers { get; }
        List<ITwInternalLinkHandler> InternalLinkHandlers { get; }
        List<ITwExceptionHandler> ExceptionHandlers { get; }
        List<ITwCompletionHandler> CompletionHandlers { get; }

        List<TwEngineFunctionDescriptor> StandardFunctions { get; }
        List<TwEngineFunctionDescriptor> ScopeFunctions { get; }
        List<TwEngineFunctionDescriptor> ProcessingFunctions { get; }
        List<TwEngineFunctionDescriptor> PostProcessingFunctions { get; }

        Task<ITwEngineState> Transform(ITwSharedLocalizationText localizer, ITwSessionState? sessionState, ITwPage page, int? revision = null, WikiMatchType[]? omitMatches = null);
    }
}
