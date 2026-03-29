using Microsoft.Extensions.Logging;
using NTDLS.Helpers;
using System.Reflection;
using TightWiki.Plugin;
using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Interfaces;

namespace TightWiki.Engine
{
    public class TwEngine
        : ITwEngine
    {
        public TwConfiguration WikiConfiguration { get; private set; }

        public ILogger<ITwEngine> Logger { get; set; }
        public IMarkupHandler MarkupHandler { get; private set; }
        public IHeadingHandler HeadingHandler { get; private set; }
        public ICommentHandler CommentHandler { get; private set; }
        public IEmojiHandler EmojiHandler { get; private set; }
        public IExternalLinkHandler ExternalLinkHandler { get; private set; }
        public IInternalLinkHandler InternalLinkHandler { get; private set; }
        public IExceptionHandler ExceptionHandler { get; private set; }
        public ICompletionHandler CompletionHandler { get; private set; }

        public List<TwEngineFunctionModule> EngineModules { get; private set; }
        public List<TwEngineFunctionDescriptor> StandardFunctions { get; private set; }
        public List<TwEngineFunctionDescriptor> ScopeFunctions { get; private set; }
        public List<TwEngineFunctionDescriptor> ProcessingFunctions { get; private set; }
        public List<TwEngineFunctionDescriptor> PostProcessingFunctions { get; private set; }

        public TwEngine(
            TwConfiguration wikiConfiguration,
            ILogger<ITwEngine> logger,
            IMarkupHandler markupHandler,
            IHeadingHandler headingHandler,
            ICommentHandler commentHandler,
            IEmojiHandler emojiHandler,
            IExternalLinkHandler externalLinkHandler,
            IInternalLinkHandler internalLinkHandler,
            IExceptionHandler exceptionHandler,
            ICompletionHandler completionHandler)
        {
            WikiConfiguration = wikiConfiguration;
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
                    Attribute = t.GetCustomAttribute<TwFunctionModuleAttribute>()
                })
                .Where(x => x.Attribute != null)
                .Where(x => typeof(ITwFunctionModule).IsAssignableFrom(x.Type))
                //This is where we instantiate the function modules, so we can later
                //  invoke their functions without needing to instantiate them again.
                .Select(x => new TwEngineFunctionModule(x.Type, x.Attribute.EnsureNotNull()))
                .ToList();

            StandardFunctions = EngineModules
                .SelectMany(t => t.DeclaringType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                .Select(m => new
                {
                    Method = m,
                    Attribute = m.GetCustomAttribute<TwStandardFunctionAttribute>()
                })
                .Where(x => x.Attribute != null)
                .Select(x =>
                {
                    if (x.Method.ReturnType != typeof(Task<HandlerResult>))
                        throw new InvalidOperationException(
                            $"Function '{x.Method.Name}' on '{x.Method.DeclaringType?.Name}' must return Task<HandlerResult>.");
                    return x;
                })
                .Select(m => new TwEngineFunctionDescriptor(
                    EngineModules.Single(t => t.DeclaringType == m.Method.DeclaringType),
                    m.Method, m.Attribute.EnsureNotNull()))
                .ToList();

            ScopeFunctions = EngineModules
                .SelectMany(t => t.DeclaringType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                .Select(m => new
                {
                    Method = m,
                    Attribute = m.GetCustomAttribute<TwScopeFunctionAttribute>()
                })
                .Where(x => x.Attribute != null)
                .Select(x =>
                {
                    if (x.Method.ReturnType != typeof(Task<HandlerResult>))
                        throw new InvalidOperationException(
                            $"Function '{x.Method.Name}' on '{x.Method.DeclaringType?.Name}' must return Task<HandlerResult>.");
                    return x;
                })
                .Select(m => new TwEngineFunctionDescriptor(
                    EngineModules.Single(t => t.DeclaringType == m.Method.DeclaringType),
                    m.Method, m.Attribute.EnsureNotNull()))
                .ToList();

            ProcessingFunctions = EngineModules
                .SelectMany(t => t.DeclaringType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                .Select(m => new
                {
                    Method = m,
                    Attribute = m.GetCustomAttribute<TwProcessingInstructionFunctionAttribute>()
                })
                .Where(x => x.Attribute != null)
                .Select(x =>
                {
                    if (x.Method.ReturnType != typeof(Task<HandlerResult>))
                        throw new InvalidOperationException(
                            $"Function '{x.Method.Name}' on '{x.Method.DeclaringType?.Name}' must return Task<HandlerResult>.");
                    return x;
                })
                .Select(m => new TwEngineFunctionDescriptor(
                    EngineModules.Single(t => t.DeclaringType == m.Method.DeclaringType),
                    m.Method, m.Attribute.EnsureNotNull()))
                .ToList();

            PostProcessingFunctions = EngineModules
                .SelectMany(t => t.DeclaringType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                .Select(m => new
                {
                    Method = m,
                    Attribute = m.GetCustomAttribute<TwPostProcessingInstructionFunctionAttribute>()
                })
                .Where(x => x.Attribute != null)
                .Select(x =>
                {
                    if (x.Method.ReturnType != typeof(Task<HandlerResult>))
                        throw new InvalidOperationException(
                            $"Function '{x.Method.Name}' on '{x.Method.DeclaringType?.Name}' must return Task<HandlerResult>.");
                    return x;
                })
                .Select(m => new TwEngineFunctionDescriptor(
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
        public async Task<ITwEngineState> Transform(ISharedLocalizationText localizer, ISessionState? session, IWikiPage page, int? revision = null, WikiMatchType[]? omitMatches = null)
        {
            var childState = new TwEngineState(Logger, this, localizer, session, page, revision, omitMatches);
            return await childState.Transform();
        }
    }
}
