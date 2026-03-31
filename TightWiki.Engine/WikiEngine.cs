using Microsoft.Extensions.Logging;
using NTDLS.Helpers;
using System.Reflection;
using TightWiki.Engine.Module;
using TightWiki.Engine.Module.Function;
using TightWiki.Engine.Module.Handlers;
using TightWiki.Plugin;
using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Attributes.Functions;
using TightWiki.Plugin.Attributes.Handlers;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Module.Function;
using TightWiki.Plugin.Interfaces.Module.Handlers;

namespace TightWiki.Engine
{
    public class WikiEngine
        : ITwEngine
    {
        public TwConfiguration WikiConfiguration { get; private set; }

        public ILogger<ITwEngine> Logger { get; set; }
        public List<PluginModule> EngineModules { get; private set; }
        public ITwDatabaseManager DatabaseManager { get; private set; }

        public List<ITwCommentHandler> CommentHandlers { get; private set; } = new();
        public List<ITwCompletionHandler> CompletionHandlers { get; private set; } = new();
        public List<ITwEmojiHandler> EmojiHandlers { get; private set; } = new();
        public List<ITwExceptionHandler> ExceptionHandlers { get; private set; } = new();
        public List<ITwExternalLinkHandler> ExternalLinkHandlers { get; private set; } = new();
        public List<ITwHeadingHandler> HeadingHandlers { get; private set; } = new();
        public List<ITwInternalLinkHandler> InternalLinkHandlers { get; private set; } = new();
        public List<ITwMarkupHandler> MarkupHandlers { get; private set; } = new();

        public List<ITwFunctionDescriptor> PostProcessingFunctions { get; private set; } = new();
        public List<ITwFunctionDescriptor> ProcessingFunctions { get; private set; } = new();
        public List<ITwFunctionDescriptor> ScopeFunctions { get; private set; } = new();
        public List<ITwFunctionDescriptor> StandardFunctions { get; private set; } = new();

        public WikiEngine(
            TwConfiguration wikiConfiguration,
            ITwDatabaseManager databaseManager,
            ILogger<ITwEngine> logger)
        {
            WikiConfiguration = wikiConfiguration;
            DatabaseManager = databaseManager;
            Logger = logger;

            EngineModules = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Select(t => new
                {
                    Type = t,
                    Attribute = t.GetCustomAttribute<TwPluginModuleAttribute>()
                })
                .Where(x => x.Attribute != null)
                .Where(x => typeof(ITwPluginModule).IsAssignableFrom(x.Type))
                //This is where we instantiate the function modules, so we can later
                //  invoke their functions without needing to instantiate them again.
                .Select(x => new PluginModule(x.Type, x.Attribute.EnsureNotNull()))
                .ToList();

            foreach (var item in BuildFunctionDescriptors<TwStandardFunctionAttribute>(EngineModules))
                StandardFunctions.Add(item);
            foreach (var item in BuildFunctionDescriptors<TwScopeFunctionAttribute>(EngineModules))
                ScopeFunctions.Add(item);
            foreach (var item in BuildFunctionDescriptors<TwProcessingInstructionFunctionAttribute>(EngineModules))
                ProcessingFunctions.Add(item);
            foreach (var item in BuildFunctionDescriptors<TwPostProcessingInstructionFunctionAttribute>(EngineModules))
                PostProcessingFunctions.Add(item);

            foreach (var item in BuildHandlerDescriptors<TwCompletionHandlerAttribute>(EngineModules))
                CompletionHandlers.Add(new CompletionHandlerDescriptor(item));
            foreach (var item in BuildHandlerDescriptors<TwEmojiHandlerAttribute>(EngineModules))
                EmojiHandlers.Add(new EmojiHandlerDescriptor(item));
            foreach (var item in BuildHandlerDescriptors<TwExceptionHandlerAttribute>(EngineModules))
                ExceptionHandlers.Add(new ExceptionHandlerDescriptor(item));
            foreach (var item in BuildHandlerDescriptors<TwExternalLinkHandlerAttribute>(EngineModules))
                ExternalLinkHandlers.Add(new ExternalLinkHandlerDescriptor(item));
            foreach (var item in BuildHandlerDescriptors<TwHeadingHandlerAttribute>(EngineModules))
                HeadingHandlers.Add(new HeadingHandlerDescriptor(item));
            foreach (var item in BuildHandlerDescriptors<TwInternalLinkHandlerAttribute>(EngineModules))
                InternalLinkHandlers.Add(new InternalLinkHandlerDescriptor(item));
            foreach (var item in BuildHandlerDescriptors<TwMarkupHandlerAttribute>(EngineModules))
                MarkupHandlers.Add(new MarkupHandlerDescriptor(item));
            foreach (var item in BuildHandlerDescriptors<TwCommentHandlerAttribute>(EngineModules))
                CommentHandlers.Add(new CommentHandlerDescriptor(item));
        }

        private static List<FunctionDescriptor> BuildFunctionDescriptors<TFunctionAttribute>(List<PluginModule> pluginModules)
            where TFunctionAttribute : Attribute, ITwFunctionDescriptorAttribute
        {
            return pluginModules
                .SelectMany(module => module.DeclaringType
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Select(m => new { Method = m, Module = module }))
                .Select(x => new
                {
                    x.Method,
                    x.Module,
                    PluginAttribute = x.Method.DeclaringType?.GetCustomAttribute<TwPluginModuleAttribute>(),
                    Attribute = x.Method.GetCustomAttribute<TFunctionAttribute>()
                })
                .Where(x => x.Attribute != null && x.PluginAttribute != null)
                .Select(x =>
                {
                    if (x.PluginAttribute == null)
                        throw new InvalidOperationException($"Function '{x.Method.Name}' on '{x.Method.DeclaringType?.Name}' must belong to a class decorated with TwPluginModuleAttribute.");
                    if (x.Method.ReturnType != typeof(Task<TwPluginResult>))
                        throw new InvalidOperationException($"Function '{x.Method.Name}' on '{x.Method.DeclaringType?.Name}' must return Task<HandlerResult>.");
                    return x;
                })
                .Select(x => new FunctionDescriptor(x.Module, x.Method, x.Attribute.EnsureNotNull(), x.PluginAttribute.EnsureNotNull()))
                .OrderBy(o => o.ModuleAttribute.Order).ThenBy(o => o.Attribute.IsFirstChance)
                .ToList();
        }

        private static List<HandlerDescriptor> BuildHandlerDescriptors<TFunctionAttribute>(List<PluginModule> pluginModules)
            where TFunctionAttribute : Attribute, ITwHandlerDescriptorAttribute
        {
            return pluginModules
                .SelectMany(module => module.DeclaringType
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Select(m => new { Method = m, Module = module }))
                .Select(x => new
                {
                    x.Method,
                    x.Module,
                    PluginAttribute = x.Method.DeclaringType?.GetCustomAttribute<TwPluginModuleAttribute>(),
                    Attribute = x.Method.GetCustomAttribute<TFunctionAttribute>()
                })
                .Where(x => x.Attribute != null && x.PluginAttribute != null)
                .Select(x =>
                {
                    if (x.PluginAttribute == null)
                        throw new InvalidOperationException($"Function '{x.Method.Name}' on '{x.Method.DeclaringType?.Name}' must belong to a class decorated with TwPluginModuleAttribute.");
                    if (x.Method.ReturnType != typeof(Task<TwPluginResult>))
                        throw new InvalidOperationException($"Function '{x.Method.Name}' on '{x.Method.DeclaringType?.Name}' must return Task<HandlerResult>.");
                    return x;
                })
                .Select(x => new HandlerDescriptor(x.Module, x.Method, x.Attribute.EnsureNotNull(), x.PluginAttribute.EnsureNotNull()))
                .OrderBy(o => o.ModuleAttribute.Order)
                .ToList();
        }

        /*

        private static List<TwEngineHandlerDescriptor> BuildhanderDescriptors<TFunctionAttribute>(
    List<TwEngineFunctionModule> pluginModules)
    where TFunctionAttribute : Attribute, ITwHandlerDescriptorAttribute
        {
            return pluginModules
                .SelectMany(module => module.DeclaringType
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Select(m => new { Method = m, Module = module }))
                .Select(x => new
                {
                    x.Method,
                    x.Module,
                    PluginAttribute = x.Method.DeclaringType?.GetCustomAttribute<TwPluginModuleAttribute>(),
                    Attribute = x.Method.GetCustomAttribute<TFunctionAttribute>()
                })
                .Where(x => x.Attribute != null)
                .Select(x =>
                {
                    if (x.PluginAttribute == null)
                        throw new InvalidOperationException(
                            $"Function '{x.Method.Name}' on '{x.Method.DeclaringType?.Name}' must belong to a class decorated with TwPluginModuleAttribute.");
                    if (x.Method.ReturnType != typeof(Task<TwHandlerResult>))
                        throw new InvalidOperationException(
                            $"Function '{x.Method.Name}' on '{x.Method.DeclaringType?.Name}' must return Task<HandlerResult>.");
                    return x;
                })
                .Select(x => new TwEngineHandlerDescriptor(x.Module, x.Method, x.Attribute.EnsureNotNull()))
                .ToList();
        }
        */

        /// <summary>
        /// Transforms the content for the given page.
        /// </summary>
        /// <param name="localizer">The localization text provider.</param>
        /// <param name="session">The users current state, used for localization.</param>
        /// <param name="page">The page that is being processed.</param>
        /// <param name="revision">The revision of the page that is being processed.</param>
        /// <param name="omitMatches">The type of matches that we want to omit from processing.</param>
        public async Task<ITwEngineState> Transform(ITwSharedLocalizationText localizer, ITwSessionState? session, ITwPage page, int? revision = null, WikiMatchType[]? omitMatches = null)
        {
            var childState = new WikiEngineState(Logger, this, localizer, session, page, revision, omitMatches);
            return await childState.Transform();
        }
    }
}
