using Microsoft.Extensions.Logging;
using TightWiki.Library;
using TightWiki.Library.Interfaces;
using static TightWiki.Engine.Library.Constants;

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

        List<TightEngineFunctionEnvelope> StandardFunctions { get; }
        List<TightEngineFunctionEnvelope> ScopeFunctions { get; }
        List<TightEngineFunctionEnvelope> ProcessingFunctions { get; }
        List<TightEngineFunctionEnvelope> PostProcessingFunctions { get; }

        Task<ITightEngineState> Transform(ISharedLocalizationText localizer, ISessionState? sessionState, IWikiPage page, int? revision = null, WikiMatchType[]? omitMatches = null);
    }
}
