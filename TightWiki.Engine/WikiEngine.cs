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
using TightWiki.Plugin.Models;

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
        public List<ITwExceptionPlugin> ExceptionHandlers { get; private set; } = new();
        public List<ITwHandlerDescriptor> MarkupHandlers { get; private set; } = new();

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
                item.Plugin.Functions.Add(item);
                StandardFunctions.Add(item);
            }

            foreach (var item in BuildFunctionDescriptors<TwScopeFunctionPluginAttribute>(Plugins))
            {
                item.Plugin.Functions.Add(item);
                ScopeFunctions.Add(item);
            }

            foreach (var item in BuildFunctionDescriptors<TwProcessingInstructionFunctionPluginAttribute>(Plugins))
            {
                item.Plugin.Functions.Add(item);
                ProcessingFunctions.Add(item);
            }

            foreach (var item in BuildHandlerDescriptors<TwCompletionPluginHandlerAttribute>(Plugins))
            {
                item.Plugin.Handlers.Add(item);
                CompletionHandlers.Add(new CompletionHandlerDescriptor(item));
            }

            foreach (var item in BuildHandlerDescriptors<TwExceptionPluginHandlerAttribute>(Plugins))
            {
                item.Plugin.Handlers.Add(item);
                ExceptionHandlers.Add(new ExceptionHandlerDescriptor(item));
            }

            foreach (var item in BuildHandlerDescriptors<TwMarkupPluginHandlerAttribute>(Plugins))
            {
                var expressionAttributes = item.Method.GetCustomAttributes<TwPluginRegularExpressionAttribute>();
                if (expressionAttributes.Any())
                {
                    item.Expressions.AddRange(expressionAttributes);
                }

                item.Plugin.Handlers.Add(item);
                MarkupHandlers.Add(item);
            }
        }

        private static List<FunctionDescriptor> BuildFunctionDescriptors<TFunctionAttribute>(List<ITwPlugin> pluginModules)
            where TFunctionAttribute : Attribute, ITwPluginFunctionAttribute
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
                .ThenBy(o => o.PluginAttribute.Precedence)
                .ThenBy(o => o.FunctionAttribute.Precedence)
                .ToList();
        }

        private static List<MarkupHandlerDescriptor> BuildHandlerDescriptors<TFunctionAttribute>(List<ITwPlugin> pluginModules)
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
                .Select(x => new MarkupHandlerDescriptor(x.Module, x.Method, x.Attribute.EnsureNotNull(), x.PluginAttribute.EnsureNotNull()))
                .OrderBy(o => o.PluginAttribute.Precedence)
                .ThenBy(o => o.HandlerAttribute.Precedence)
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

        /// <summary>
        /// Transforms the content for the given string markup.
        /// </summary>
        /// <param name="localizer">The localization text provider.</param>
        /// <param name="session">The users current state, used for localization.</param>
        /// <param name="markup">The markup content that is being processed.</param>
        public async Task<ITwEngineState> Transform(ITwSharedLocalizationText localizer, ITwSessionState? session, string markup)
        {
            var page = new TwPage()
            {
                Body = markup,
                Name = "adhoc"
            };

            var childState = new WikiEngineState(Logger, this, localizer, session, page);
            return await childState.Transform();
        }
    }
}
