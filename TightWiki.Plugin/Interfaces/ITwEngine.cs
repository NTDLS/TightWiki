using Microsoft.Extensions.Logging;
using TightWiki.Plugin.Engine.Function;
using TightWiki.Plugin.Engine.Handlers;

namespace TightWiki.Plugin.Interfaces
{
    public interface ITwEngine
    {
        TwConfiguration WikiConfiguration { get; }

        ITwDatabaseManager DatabaseManager { get; }

        ILogger<ITwEngine> Logger { get; }

        List<TwCommentHandlerDescriptor> CommentHandlers { get; }
        List<TwCompletionHandlerDescriptor> CompletionHandlers { get; }
        List<TwEmojiHandlerDescriptor> EmojiHandlers { get; }
        List<TwExceptionHandlerDescriptor> ExceptionHandlers { get; }
        List<TwExternalLinkHandlerDescriptor> ExternalLinkHandlers { get; }
        List<TwHeadingHandlerDescriptor> HeadingHandlers { get; }
        List<TwInternalLinkHandlerDescriptor> InternalLinkHandlers { get; }
        List<TwMarkupHandlerDescriptor> MarkupHandlers { get; }

        List<ITwEngineFunctionDescriptor> StandardFunctions { get; }
        List<ITwEngineFunctionDescriptor> ScopeFunctions { get; }
        List<ITwEngineFunctionDescriptor> ProcessingFunctions { get; }
        List<ITwEngineFunctionDescriptor> PostProcessingFunctions { get; }

        Task<ITwEngineState> Transform(ITwSharedLocalizationText localizer, ITwSessionState? sessionState, ITwPage page, int? revision = null, WikiMatchType[]? omitMatches = null);
    }
}
