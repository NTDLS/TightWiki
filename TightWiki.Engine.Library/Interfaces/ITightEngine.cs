using Microsoft.Extensions.Logging;
using TightWiki.Library;
using TightWiki.Library.Interfaces;
using static TightWiki.Engine.Library.Constants;

namespace TightWiki.Engine.Library.Interfaces
{
    public interface ITightEngine
    {
        ILogger<ITightEngine> Logger { get; }
        IScopeFunctionHandler ScopeFunctionHandler { get; }
        IStandardFunctionHandler StandardFunctionHandler { get; }
        IProcessingInstructionFunctionHandler ProcessingInstructionFunctionHandler { get; }
        IPostProcessingFunctionHandler PostProcessingFunctionHandler { get; }
        IMarkupHandler MarkupHandler { get; }
        IHeadingHandler HeadingHandler { get; }
        ICommentHandler CommentHandler { get; }
        IEmojiHandler EmojiHandler { get; }
        IExternalLinkHandler ExternalLinkHandler { get; }
        IInternalLinkHandler InternalLinkHandler { get; }
        IExceptionHandler ExceptionHandler { get; }
        ICompletionHandler CompletionHandler { get; }
        Task<ITightEngineState> Transform(ISharedLocalizationText localizer, ISessionState? sessionState, IWikiPage page, int? revision = null, WikiMatchType[]? omitMatches = null);
        //ITightEngineState TransformChild(ITightEngineState parent, IPage page, int? revision = null);
    }
}
