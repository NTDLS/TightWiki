using Microsoft.Extensions.Logging;
using NTDLS.Helpers;
using System.Reflection;
using TightWiki.Engine.Library;
using TightWiki.Engine.Library.Function.Attributes;
using TightWiki.Engine.Library.Interfaces;
using TightWiki.Library;
using TightWiki.Library.Interfaces;

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

        public List<TightEngineFunctionModule> EngineModules { get; private set; }
        public List<TightEngineFunctionDescriptor> StandardFunctions { get; private set; }
        public List<TightEngineFunctionDescriptor> ScopeFunctions { get; private set; }
        public List<TightEngineFunctionDescriptor> ProcessingFunctions { get; private set; }
        public List<TightEngineFunctionDescriptor> PostProcessingFunctions { get; private set; }

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

            EngineModules = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Select(t => new
                {
                    Type = t,
                    Attribute = t.GetCustomAttribute<TightWikiFunctionModuleAttribute>()
                })
                .Where(x => x.Attribute != null)
                .Where(x => typeof(ITightWikiFunctionModule).IsAssignableFrom(x.Type))
                //This is where we instantiate the function modules, so we can later
                //  invoke their functions without needing to instantiate them again.
                .Select(x => new TightEngineFunctionModule(x.Type, x.Attribute.EnsureNotNull()))
                .ToList();

            StandardFunctions = EngineModules
                .SelectMany(t => t.DeclaringType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                .Select(m => new
                {
                    Method = m,
                    Attribute = m.GetCustomAttribute<TightWikiStandardFunctionAttribute>()
                })
                .Where(x => x.Attribute != null)
                .Select(x =>
                {
                    if (x.Method.ReturnType != typeof(Task<HandlerResult>))
                        throw new InvalidOperationException(
                            $"Function '{x.Method.Name}' on '{x.Method.DeclaringType?.Name}' must return Task<HandlerResult>.");
                    return x;
                })
                .Select(m => new TightEngineFunctionDescriptor(
                    EngineModules.Single(t => t.DeclaringType == m.Method.DeclaringType),
                    m.Method, m.Attribute.EnsureNotNull()))
                .ToList();

            ScopeFunctions = EngineModules
                .SelectMany(t => t.DeclaringType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                .Select(m => new
                {
                    Method = m,
                    Attribute = m.GetCustomAttribute<TightWikiScopeFunctionAttribute>()
                })
                .Where(x => x.Attribute != null)
                .Select(x =>
                {
                    if (x.Method.ReturnType != typeof(Task<HandlerResult>))
                        throw new InvalidOperationException(
                            $"Function '{x.Method.Name}' on '{x.Method.DeclaringType?.Name}' must return Task<HandlerResult>.");
                    return x;
                })
                .Select(m => new TightEngineFunctionDescriptor(
                    EngineModules.Single(t => t.DeclaringType == m.Method.DeclaringType),
                    m.Method, m.Attribute.EnsureNotNull()))
                .ToList();

            ProcessingFunctions = EngineModules
                .SelectMany(t => t.DeclaringType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                .Select(m => new
                {
                    Method = m,
                    Attribute = m.GetCustomAttribute<TightWikiProcessingInstructionFunctionAttribute>()
                })
                .Where(x => x.Attribute != null)
                .Select(x =>
                {
                    if (x.Method.ReturnType != typeof(Task<HandlerResult>))
                        throw new InvalidOperationException(
                            $"Function '{x.Method.Name}' on '{x.Method.DeclaringType?.Name}' must return Task<HandlerResult>.");
                    return x;
                })
                .Select(m => new TightEngineFunctionDescriptor(
                    EngineModules.Single(t => t.DeclaringType == m.Method.DeclaringType),
                    m.Method, m.Attribute.EnsureNotNull()))
                .ToList();

            PostProcessingFunctions = EngineModules
                .SelectMany(t => t.DeclaringType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                .Select(m => new
                {
                    Method = m,
                    Attribute = m.GetCustomAttribute<TightWikiPostProcessingInstructionFunctionAttribute>()
                })
                .Where(x => x.Attribute != null)
                .Select(x =>
                {
                    if (x.Method.ReturnType != typeof(Task<HandlerResult>))
                        throw new InvalidOperationException(
                            $"Function '{x.Method.Name}' on '{x.Method.DeclaringType?.Name}' must return Task<HandlerResult>.");
                    return x;
                })
                .Select(m => new TightEngineFunctionDescriptor(
                    EngineModules.Single(t => t.DeclaringType == m.Method.DeclaringType),
                    m.Method, m.Attribute.EnsureNotNull()))
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
