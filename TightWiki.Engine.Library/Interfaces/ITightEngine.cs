using Microsoft.Extensions.Logging;
using TightWiki.Library;
using TightWiki.Library.Interfaces;

namespace TightWiki.Engine.Library.Interfaces
{
    public interface ITightEngine
    {
        ILogger<ITightEngine> Logger { get; }
        IMarkupHandler MarkupHandler { get; }
        IHeadingHandler HeadingHandler { get; }
        ICommentHandler CommentHandler { get; }
        IEmojiHandler EmojiHandler { get; }
        IExternalLinkHandler ExternalLinkHandler { get; }
        IInternalLinkHandler InternalLinkHandler { get; }
        IExceptionHandler ExceptionHandler { get; }
        ICompletionHandler CompletionHandler { get; }

        List<TightEngineFunctionDescriptor> StandardFunctions { get; }
        List<TightEngineFunctionDescriptor> ScopeFunctions { get; }
        List<TightEngineFunctionDescriptor> ProcessingFunctions { get; }
        List<TightEngineFunctionDescriptor> PostProcessingFunctions { get; }

        Task<ITightEngineState> Transform(ISharedLocalizationText localizer, ISessionState? sessionState, IWikiPage page, int? revision = null, WikiMatchType[]? omitMatches = null);
    }
}
