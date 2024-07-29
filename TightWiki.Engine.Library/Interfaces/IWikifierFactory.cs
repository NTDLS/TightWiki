using TightWiki.Library.Interfaces;
using static TightWiki.Engine.Library.Constants;

namespace TightWiki.Engine.Library.Interfaces
{
    public interface IWikifierFactory
    {
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
        int CurrentNestLevel { get; }

        IWikifier Process(ISessionState? sessionState, IPage page, int? revision = null, WikiMatchType[]? omitMatches = null);
        IWikifierFactory CreateChild(IPage page);
    }
}
