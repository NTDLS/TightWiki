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

        List<TwEngineFunctionDescriptor> StandardFunctions { get; }
        List<TwEngineFunctionDescriptor> ScopeFunctions { get; }
        List<TwEngineFunctionDescriptor> ProcessingFunctions { get; }
        List<TwEngineFunctionDescriptor> PostProcessingFunctions { get; }

        Task<ITwEngineState> Transform(ITwSharedLocalizationText localizer, ITwSessionState? sessionState, ITwPage page, int? revision = null, WikiMatchType[]? omitMatches = null);
    }
}
