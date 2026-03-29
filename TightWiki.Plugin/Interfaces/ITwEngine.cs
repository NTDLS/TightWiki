using Microsoft.Extensions.Logging;

namespace TightWiki.Plugin.Interfaces
{
    public interface ITwEngine
    {
        TwConfiguration WikiConfiguration { get; }

        ILogger<ITwEngine> Logger { get; }
        IMarkupHandler MarkupHandler { get; }
        IHeadingHandler HeadingHandler { get; }
        ICommentHandler CommentHandler { get; }
        IEmojiHandler EmojiHandler { get; }
        IExternalLinkHandler ExternalLinkHandler { get; }
        IInternalLinkHandler InternalLinkHandler { get; }
        IExceptionHandler ExceptionHandler { get; }
        ICompletionHandler CompletionHandler { get; }

        List<TwEngineFunctionDescriptor> StandardFunctions { get; }
        List<TwEngineFunctionDescriptor> ScopeFunctions { get; }
        List<TwEngineFunctionDescriptor> ProcessingFunctions { get; }
        List<TwEngineFunctionDescriptor> PostProcessingFunctions { get; }

        Task<ITwEngineState> Transform(ISharedLocalizationText localizer, ISessionState? sessionState, IWikiPage page, int? revision = null, WikiMatchType[]? omitMatches = null);
    }
}
