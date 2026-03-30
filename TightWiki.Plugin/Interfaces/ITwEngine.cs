using Microsoft.Extensions.Logging;
using TightWiki.Plugin.Engine.Function;

namespace TightWiki.Plugin.Interfaces
{
    public interface ITwEngine
    {
        TwConfiguration WikiConfiguration { get; }

        ILogger<ITwEngine> Logger { get; }
        ITwMarkupHandler MarkupHandler { get; }
        ITwHeadingHandler HeadingHandler { get; }
        ITwCommentHandler CommentHandler { get; }
        ITwEmojiHandler EmojiHandler { get; }
        ITwExternalLinkHandler ExternalLinkHandler { get; }
        ITwInternalLinkHandler InternalLinkHandler { get; }
        ITwExceptionHandler ExceptionHandler { get; }
        ITwCompletionHandler CompletionHandler { get; }

        List<TwEngineFunctionDescriptor> StandardFunctions { get; }
        List<TwEngineFunctionDescriptor> ScopeFunctions { get; }
        List<TwEngineFunctionDescriptor> ProcessingFunctions { get; }
        List<TwEngineFunctionDescriptor> PostProcessingFunctions { get; }

        Task<ITwEngineState> Transform(ITwSharedLocalizationText localizer, ITwSessionState? sessionState, ITwPage page, int? revision = null, WikiMatchType[]? omitMatches = null);
    }
}
