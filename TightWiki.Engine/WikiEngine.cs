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
using TightWiki.Plugin.Interfaces.Module;
using TightWiki.Plugin.Interfaces.Module.Function;
using TightWiki.Plugin.Interfaces.Module.Handlers;

namespace TightWiki.Engine
{
    public class WikiEngine
        : ITwEngine
    {
        public TwConfiguration WikiConfiguration { get; private set; }

        public ILogger<ITwEngine> Logger { get; set; }
        public List<ITwPlugin> Plugins { get; private set; } = new();
        public ITwDatabaseManager DatabaseManager { get; private set; }

        public List<ITwCommentPlugin> CommentHandlers { get; private set; } = new();
        public List<ITwCompletionPlugin> CompletionHandlers { get; private set; } = new();
        public List<ITwEmojiPlugin> EmojiHandlers { get; private set; } = new();
        public List<ITwExceptionPlugin> ExceptionHandlers { get; private set; } = new();
        public List<ITwExternalLinkPlugin> ExternalLinkHandlers { get; private set; } = new();
        public List<ITwHeadingPlugin> HeadingHandlers { get; private set; } = new();
        public List<ITwInternalLinkPlugin> InternalLinkHandlers { get; private set; } = new();
        public List<ITwMarkupPlugin> MarkupHandlers { get; private set; } = new();

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

            var plugins = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Select(t => new
                {
                    Type = t,
                    Attribute = t.GetCustomAttribute<TwPluginAttribute>()
                })
                .Where(x => x.Attribute != null)
                //This is where we instantiate the function modules, so we can later
                //  invoke their functions without needing to instantiate them again.
                .Select(x => new PluginDescriptor(x.Type, x.Attribute.EnsureNotNull()))
                .ToList();

            foreach (var plugin in plugins)
            {
                Plugins.Add(plugin);
            }

            foreach (var item in BuildFunctionDescriptors<TwStandardFunctionPluginAttribute>(Plugins))
            {
                item.Plugin.Functions.Add(item.FunctionAttribute);
                StandardFunctions.Add(item);
            }
            foreach (var item in BuildFunctionDescriptors<TwScopeFunctionPluginAttribute>(Plugins))
            {
                item.Plugin.Functions.Add(item.FunctionAttribute);
                ScopeFunctions.Add(item);
            }
            foreach (var item in BuildFunctionDescriptors<TwProcessingInstructionFunctionPluginAttribute>(Plugins))
            {
                item.Plugin.Functions.Add(item.FunctionAttribute);
                ProcessingFunctions.Add(item);
            }
            foreach (var item in BuildFunctionDescriptors<TwPostProcessingInstructionFunctionPluginAttribute>(Plugins))
            {
                item.Plugin.Functions.Add(item.FunctionAttribute);
                PostProcessingFunctions.Add(item);
            }

            foreach (var item in BuildHandlerDescriptors<TwCompletionPluginHandlerAttribute>(Plugins))
            {
                item.Plugin.Handlers.Add(item.HandlerAttribute);
                CompletionHandlers.Add(new CompletionHandlerDescriptor(item));
            }
            foreach (var item in BuildHandlerDescriptors<TwEmojiPluginHandlerAttribute>(Plugins))
            {
                item.Plugin.Handlers.Add(item.HandlerAttribute);
                EmojiHandlers.Add(new EmojiHandlerDescriptor(item));
            }
            foreach (var item in BuildHandlerDescriptors<TwExceptionPluginHandlerAttribute>(Plugins))
            {
                item.Plugin.Handlers.Add(item.HandlerAttribute);
                ExceptionHandlers.Add(new ExceptionHandlerDescriptor(item));
            }
            foreach (var item in BuildHandlerDescriptors<TwExternalLinkPluginHandlerAttribute>(Plugins))
            {
                item.Plugin.Handlers.Add(item.HandlerAttribute);
                ExternalLinkHandlers.Add(new ExternalLinkHandlerDescriptor(item));
            }
            foreach (var item in BuildHandlerDescriptors<TwHeadingPluginHandlerAttribute>(Plugins))
            {
                item.Plugin.Handlers.Add(item.HandlerAttribute);
                HeadingHandlers.Add(new HeadingHandlerDescriptor(item));
            }
            foreach (var item in BuildHandlerDescriptors<TwInternalLinkPluginHandlerAttribute>(Plugins))
            {
                item.Plugin.Handlers.Add(item.HandlerAttribute);
                InternalLinkHandlers.Add(new InternalLinkHandlerDescriptor(item));
            }
            foreach (var item in BuildHandlerDescriptors<TwMarkupPluginHandlerAttribute>(Plugins))
            {
                item.Plugin.Handlers.Add(item.HandlerAttribute);
                MarkupHandlers.Add(new MarkupHandlerDescriptor(item));
            }
            foreach (var item in BuildHandlerDescriptors<TwCommentPluginHandlerAttribute>(Plugins))
            {
                item.Plugin.Handlers.Add(item.HandlerAttribute);
                CommentHandlers.Add(new CommentHandlerDescriptor(item));
            }
        }

        private static List<FunctionDescriptor> BuildFunctionDescriptors<TFunctionAttribute>(List<ITwPlugin> pluginModules)
            where TFunctionAttribute : Attribute, ITwFunctionPluginAttribute
        {
            return pluginModules
                .SelectMany(module => module.DeclaringType
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Select(m => new { Method = m, Module = module }))
                .Select(x => new
                {
                    x.Method,
                    x.Module,
                    PluginAttribute = x.Method.DeclaringType?.GetCustomAttribute<TwPluginAttribute>(),
                    Attribute = x.Method.GetCustomAttribute<TFunctionAttribute>()
                })
                .Where(x => x.Attribute != null && x.PluginAttribute != null)
                .Select(x =>
                {
                    if (x.PluginAttribute == null)
                        throw new InvalidOperationException($"Function '{x.Method.Name}' on '{x.Method.DeclaringType?.Name}' must belong to a class decorated with TwPluginAttribute.");
                    if (x.Method.ReturnType != typeof(Task<TwPluginResult>))
                        throw new InvalidOperationException($"Function '{x.Method.Name}' on '{x.Method.DeclaringType?.Name}' must return Task<HandlerResult>.");
                    return x;
                })
                .Select(x => new FunctionDescriptor(x.Module, x.Method, x.Attribute.EnsureNotNull(), x.PluginAttribute.EnsureNotNull()))
                .OrderByDescending(o => o.FunctionAttribute.IsFirstChance)
                .ThenBy(o => o.PluginAttribute.Order)
                .ThenBy(o => o.FunctionAttribute.Order)
                .ToList();
        }

        private static List<HandlerDescriptor> BuildHandlerDescriptors<TFunctionAttribute>(List<ITwPlugin> pluginModules)
            where TFunctionAttribute : Attribute, ITwPluginHandlerAttribute
        {
            return pluginModules
                .SelectMany(module => module.DeclaringType
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Select(m => new { Method = m, Module = module }))
                .Select(x => new
                {
                    x.Method,
                    x.Module,
                    PluginAttribute = x.Method.DeclaringType?.GetCustomAttribute<TwPluginAttribute>(),
                    Attribute = x.Method.GetCustomAttribute<TFunctionAttribute>()
                })
                .Where(x => x.Attribute != null && x.PluginAttribute != null)
                .Select(x =>
                {
                    if (x.PluginAttribute == null)
                        throw new InvalidOperationException($"Function '{x.Method.Name}' on '{x.Method.DeclaringType?.Name}' must belong to a class decorated with TwPluginAttribute.");
                    if (x.Method.ReturnType != typeof(Task<TwPluginResult>))
                        throw new InvalidOperationException($"Function '{x.Method.Name}' on '{x.Method.DeclaringType?.Name}' must return Task<HandlerResult>.");
                    return x;
                })
                .Select(x => new HandlerDescriptor(x.Module, x.Method, x.Attribute.EnsureNotNull(), x.PluginAttribute.EnsureNotNull()))
                .OrderBy(o => o.PluginAttribute.Order)
                .ThenBy(o => o.HandlerAttribute.Order)
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
        public async Task<ITwEngineState> Transform(ITwSharedLocalizationText localizer, ITwSessionState? session, ITwPage page, int? revision = null, TwMatchType[]? omitMatches = null)
        {
            var childState = new WikiEngineState(Logger, this, localizer, session, page, revision, omitMatches);
            return await childState.Transform();
        }
    }
}
