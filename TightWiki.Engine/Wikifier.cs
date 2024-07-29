using TightWiki.Engine.Library.Interfaces;
using TightWiki.Library.Interfaces;
using static TightWiki.Engine.Library.Constants;

namespace TightWiki.Engine
{
    public class Wikifier : IWikifier
    {
        public IScopeFunctionHandler ScopeFunctionHandler { get; private set; }
        public IStandardFunctionHandler StandardFunctionHandler { get; private set; }
        public IProcessingInstructionFunctionHandler ProcessingInstructionFunctionHandler { get; private set; }
        public IPostProcessingFunctionHandler PostProcessingFunctionHandler { get; private set; }
        public IMarkupHandler MarkupHandler { get; private set; }
        public IHeadingHandler HeadingHandler { get; private set; }
        public ICommentHandler CommentHandler { get; private set; }
        public IEmojiHandler EmojiHandler { get; private set; }
        public IExternalLinkHandler ExternalLinkHandler { get; private set; }
        public IInternalLinkHandler InternalLinkHandler { get; private set; }
        public IExceptionHandler ExceptionHandler { get; private set; }
        public ICompletionHandler CompletionHandler { get; private set; }
        public int CurrentNestLevel { get; private set; }

        public Wikifier(
            IStandardFunctionHandler standardFunctionHandler,
            IScopeFunctionHandler scopeFunctionHandler,
            IProcessingInstructionFunctionHandler processingInstructionFunctionHandler,
            IPostProcessingFunctionHandler postProcessingFunctionHandler,
            IMarkupHandler markupHandler,
            IHeadingHandler headingHandler,
            ICommentHandler commentHandler,
            IEmojiHandler emojiHandler,
            IExternalLinkHandler externalLinkHandler,
            IInternalLinkHandler internalLinkHandler,
            IExceptionHandler exceptionHandler,
            ICompletionHandler completionHandler,
            int nestLevel = 0)
        {
            StandardFunctionHandler = standardFunctionHandler;

            StandardFunctionHandler = standardFunctionHandler;
            ScopeFunctionHandler = scopeFunctionHandler;
            ProcessingInstructionFunctionHandler = processingInstructionFunctionHandler;
            PostProcessingFunctionHandler = postProcessingFunctionHandler;
            MarkupHandler = markupHandler;
            HeadingHandler = headingHandler;
            CommentHandler = commentHandler;
            EmojiHandler = emojiHandler;
            ExternalLinkHandler = externalLinkHandler;
            InternalLinkHandler = internalLinkHandler;
            ExceptionHandler = exceptionHandler;
            CompletionHandler = completionHandler;

            CurrentNestLevel = nestLevel;
        }

        public IWikifierSession Process(ISessionState? sessionState, IPage page, int? revision = null, WikiMatchType[]? omitMatches = null)
        {
            var wikifierSession = new WikifierSession(this, sessionState, page, revision, omitMatches);
            wikifierSession.Process();
            return wikifierSession;
        }

        public IWikifier CreateChild(IPage page)
        {
            return new Wikifier(StandardFunctionHandler,
                ScopeFunctionHandler,
                ProcessingInstructionFunctionHandler,
                PostProcessingFunctionHandler,
                MarkupHandler,
                HeadingHandler,
                CommentHandler,
                EmojiHandler,
                ExternalLinkHandler,
                InternalLinkHandler,
                ExceptionHandler,
                CompletionHandler,
                CurrentNestLevel + 1
            );
        }
    }
}
