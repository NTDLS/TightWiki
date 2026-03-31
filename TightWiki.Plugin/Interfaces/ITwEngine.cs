using Microsoft.Extensions.Logging;
using TightWiki.Plugin.Interfaces.Module.Function;
using TightWiki.Plugin.Interfaces.Module.Handlers;

namespace TightWiki.Plugin.Interfaces
{
    public interface ITwEngine
    {
        TwConfiguration WikiConfiguration { get; }

        ITwDatabaseManager DatabaseManager { get; }

        ILogger<ITwEngine> Logger { get; }

        List<ITwCommentHandler> CommentHandlers { get; }
        List<ITwCompletionHandler> CompletionHandlers { get; }
        List<ITwEmojiHandler> EmojiHandlers { get; }
        List<ITwExceptionHandler> ExceptionHandlers { get; }
        List<ITwExternalLinkHandler> ExternalLinkHandlers { get; }
        List<ITwHeadingHandler> HeadingHandlers { get; }
        List<ITwInternalLinkHandler> InternalLinkHandlers { get; }
        List<ITwMarkupHandler> MarkupHandlers { get; }

        List<ITwEngineFunctionDescriptor> StandardFunctions { get; }
        List<ITwEngineFunctionDescriptor> ScopeFunctions { get; }
        List<ITwEngineFunctionDescriptor> ProcessingFunctions { get; }
        List<ITwEngineFunctionDescriptor> PostProcessingFunctions { get; }

        Task<ITwEngineState> Transform(ITwSharedLocalizationText localizer, ITwSessionState? sessionState, ITwPage page, int? revision = null, WikiMatchType[]? omitMatches = null);
    }
}
