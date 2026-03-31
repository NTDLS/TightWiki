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

        List<ITwCommentPlugin> CommentHandlers { get; }
        List<ITwCompletionPlugin> CompletionHandlers { get; }
        List<ITwEmojiPlugin> EmojiHandlers { get; }
        List<ITwExceptionPlugin> ExceptionHandlers { get; }
        List<ITwExternalLinkPlugin> ExternalLinkHandlers { get; }
        List<ITwHeadingPlugin> HeadingHandlers { get; }
        List<ITwInternalLinkPlugin> InternalLinkHandlers { get; }
        List<ITwMarkupPlugin> MarkupHandlers { get; }

        List<ITwFunctionDescriptor> StandardFunctions { get; }
        List<ITwFunctionDescriptor> ScopeFunctions { get; }
        List<ITwFunctionDescriptor> ProcessingFunctions { get; }
        List<ITwFunctionDescriptor> PostProcessingFunctions { get; }

        Task<ITwEngineState> Transform(ITwSharedLocalizationText localizer, ITwSessionState? sessionState, ITwPage page, int? revision = null, TwMatchType[]? omitMatches = null);
    }
}
