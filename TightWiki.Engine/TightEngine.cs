using Microsoft.Extensions.Logging;
using NTDLS.Helpers;
using System.Reflection;
using TightWiki.Engine.Library;
using TightWiki.Engine.Library.Ae.Engine.Metadata;
using TightWiki.Engine.Library.Interfaces;
using TightWiki.Library;
using TightWiki.Library.Interfaces;
using static TightWiki.Engine.Library.Constants;

namespace TightWiki.Engine
{
    public class TightEngine
        : ITightEngine
    {
        public ILogger<ITightEngine> Logger { get; set; }
        public IMarkupHandler MarkupHandler { get; private set; }
        public IHeadingHandler HeadingHandler { get; private set; }
        public ICommentHandler CommentHandler { get; private set; }
        public IEmojiHandler EmojiHandler { get; private set; }
        public IExternalLinkHandler ExternalLinkHandler { get; private set; }
        public IInternalLinkHandler InternalLinkHandler { get; private set; }
        public IExceptionHandler ExceptionHandler { get; private set; }
        public ICompletionHandler CompletionHandler { get; private set; }

        public List<TightEnginFunctionEnvelope> StandardFunctions { get; private set; }
        public List<TightEnginFunctionEnvelope> ScopeFunctions { get; private set; }
        public List<TightEnginFunctionEnvelope> ProcessingFunctions { get; private set; }
        public List<TightEnginFunctionEnvelope> PostProcessingFunctions { get; private set; }

        public TightEngine(
            ILogger<ITightEngine> logger,
            IMarkupHandler markupHandler,
            IHeadingHandler headingHandler,
            ICommentHandler commentHandler,
            IEmojiHandler emojiHandler,
            IExternalLinkHandler externalLinkHandler,
            IInternalLinkHandler internalLinkHandler,
            IExceptionHandler exceptionHandler,
            ICompletionHandler completionHandler)
        {
            Logger = logger;
            MarkupHandler = markupHandler;
            HeadingHandler = headingHandler;
            CommentHandler = commentHandler;
            EmojiHandler = emojiHandler;
            ExternalLinkHandler = externalLinkHandler;
            InternalLinkHandler = internalLinkHandler;
            ExceptionHandler = exceptionHandler;
            CompletionHandler = completionHandler;

            //Enum the various types of functions based on their attributes.

            StandardFunctions = Assembly.GetExecutingAssembly()
                .GetTypes()
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                .Select(m => new
                {
                    Method = m,
                    Attribute = m.GetCustomAttribute<TightWikiStandardFunctionAttribute>()
                })
                .Where(x => x.Attribute != null)
                .Select(m => new TightEnginFunctionEnvelope(m.Method, m.Attribute.EnsureNotNull()))
                .ToList();

            ScopeFunctions = Assembly.GetExecutingAssembly()
                .GetTypes()
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                .Select(m => new
                {
                    Method = m,
                    Attribute = m.GetCustomAttribute<TightWikiScopeFunctionAttribute>()
                })
                .Where(x => x.Attribute != null)
                .Select(m => new TightEnginFunctionEnvelope(m.Method, m.Attribute.EnsureNotNull()))
                .ToList();

            ProcessingFunctions = Assembly.GetExecutingAssembly()
                .GetTypes()
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                .Select(m => new
                {
                    Method = m,
                    Attribute = m.GetCustomAttribute<TightWikiProcessingFunctionAttribute>()
                })
                .Where(x => x.Attribute != null)
                .Select(m => new TightEnginFunctionEnvelope(m.Method, m.Attribute.EnsureNotNull()))
                .ToList();

            PostProcessingFunctions = Assembly.GetExecutingAssembly()
                .GetTypes()
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                .Select(m => new
                {
                    Method = m,
                    Attribute = m.GetCustomAttribute<TightWikiPostProcessingFunctionAttribute>()
                })
                .Where(x => x.Attribute != null)
                .Select(m => new TightEnginFunctionEnvelope(m.Method, m.Attribute.EnsureNotNull()))
                .ToList();
        }

        /// <summary>
        /// Transforms the content for the given page.
        /// </summary>
        /// <param name="localizer">The localization text provider.</param>
        /// <param name="session">The users current state, used for localization.</param>
        /// <param name="page">The page that is being processed.</param>
        /// <param name="revision">The revision of the page that is being processed.</param>
        /// <param name="omitMatches">The type of matches that we want to omit from processing.</param>
        public async Task<ITightEngineState> Transform(ISharedLocalizationText localizer, ISessionState? session, IWikiPage page, int? revision = null, WikiMatchType[]? omitMatches = null)
        {
            var childState = new TightEngineState(Logger, this, localizer, session, page, revision, omitMatches);
            return await childState.Transform();
        }
    }
}
