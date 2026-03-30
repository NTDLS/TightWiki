using Microsoft.Extensions.Logging;
using NTDLS.Helpers;
using System.Reflection;
using TightWiki.Plugin;
using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Attributes.Functions;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Engine.Function;
using TightWiki.Plugin.Engine.Handlers;
using TightWiki.Plugin.Interfaces;

namespace TightWiki.Engine
{
    public class TwEngine
        : ITwEngine
    {
        public TwConfiguration WikiConfiguration { get; private set; }

        public ILogger<ITwEngine> Logger { get; set; }
        public List<TwEnginePluginModule> EngineModules { get; private set; }
        public ITwDatabaseManager DatabaseManager { get; private set; }


        public List<TwCommentHandlerDescriptor> CommentHandlers { get; private set; }
        public List<TwCompletionHandlerDescriptor> CompletionHandlers { get; private set; }
        public List<TwEmojiHandlerDescriptor> EmojiHandlers { get; private set; }
        public List<TwExceptionHandlerDescriptor> ExceptionHandlers { get; private set; }
        public List<TwExternalLinkHandlerDescriptor> ExternalLinkHandlers { get; private set; }
        public List<TwHeadingHandlerDescriptor> HeadingHandlers { get; private set; }
        public List<TwInternalLinkHandlerDescriptor> InternalLinkHandlers { get; private set; }
        public List<TwMarkupHandlerDescriptor> MarkupHandlers { get; private set; }


        public List<TwEngineFunctionDescriptor> PostProcessingFunctions { get; private set; }
        public List<TwEngineFunctionDescriptor> ProcessingFunctions { get; private set; }
        public List<TwEngineFunctionDescriptor> ScopeFunctions { get; private set; }
        public List<TwEngineFunctionDescriptor> StandardFunctions { get; private set; }


        public TwEngine(
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
                .Select(x => new TwEnginePluginModule(x.Type, x.Attribute.EnsureNotNull()))
                .ToList();

            StandardFunctions = BuildFunctionDescriptors<TwStandardFunctionAttribute>(EngineModules);
            ScopeFunctions = BuildFunctionDescriptors<TwScopeFunctionAttribute>(EngineModules);
            ProcessingFunctions = BuildFunctionDescriptors<TwProcessingInstructionFunctionAttribute>(EngineModules);
            PostProcessingFunctions = BuildFunctionDescriptors<TwPostProcessingInstructionFunctionAttribute>(EngineModules);

            CompletionHandlers = BuildHandlerDescriptors<TwCompletionHandlerAttribute>(EngineModules)
                .Select(o => new TwCompletionHandlerDescriptor(o)).ToList();

            EmojiHandlers = BuildHandlerDescriptors<TwEmojiHandlerAttribute>(EngineModules)
                .Select(o => new TwEmojiHandlerDescriptor(o)).ToList();

            ExceptionHandlers = BuildHandlerDescriptors<TwExceptionHandlerAttribute>(EngineModules)
                .Select(o => new TwExceptionHandlerDescriptor(o)).ToList();

            ExternalLinkHandlers = BuildHandlerDescriptors<TwExternalLinkHandlerAttribute>(EngineModules)
                .Select(o => new TwExternalLinkHandlerDescriptor(o)).ToList();

            HeadingHandlers = BuildHandlerDescriptors<TwHeadingHandlerAttribute>(EngineModules)
                .Select(o => new TwHeadingHandlerDescriptor(o)).ToList();

            InternalLinkHandlers = BuildHandlerDescriptors<TwInternalLinkHandlerAttribute>(EngineModules)
                .Select(o => new TwInternalLinkHandlerDescriptor(o)).ToList();

            MarkupHandlers = BuildHandlerDescriptors<TwMarkupHandlerAttribute>(EngineModules)
                .Select(o => new TwMarkupHandlerDescriptor(o)).ToList();

            CommentHandlers = BuildHandlerDescriptors<TwCommentHandlerAttribute>(EngineModules)
                .Select(o => new TwCommentHandlerDescriptor(o)).ToList();
        }

        private static List<TwEngineFunctionDescriptor> BuildFunctionDescriptors<TFunctionAttribute>(List<TwEnginePluginModule> pluginModules)
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
                    if (x.Method.ReturnType != typeof(Task<TwHandlerResult>))
                        throw new InvalidOperationException($"Function '{x.Method.Name}' on '{x.Method.DeclaringType?.Name}' must return Task<HandlerResult>.");
                    return x;
                })
                .Select(x => new TwEngineFunctionDescriptor(x.Module, x.Method, x.Attribute.EnsureNotNull(), x.PluginAttribute.EnsureNotNull()))
                .OrderBy(o => o.ModuleAttribute.Order).ThenBy(o => o.Attribute.IsFirstChance)
                .ToList();
        }

        private static List<TwEngineHandlerDescriptor> BuildHandlerDescriptors<TFunctionAttribute>(List<TwEnginePluginModule> pluginModules)
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
                    if (x.Method.ReturnType != typeof(Task<TwHandlerResult>))
                        throw new InvalidOperationException($"Function '{x.Method.Name}' on '{x.Method.DeclaringType?.Name}' must return Task<HandlerResult>.");
                    return x;
                })
                .Select(x => new TwEngineHandlerDescriptor(x.Module, x.Method, x.Attribute.EnsureNotNull(), x.PluginAttribute.EnsureNotNull()))
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
            var childState = new TwEngineState(Logger, this, localizer, session, page, revision, omitMatches);
            return await childState.Transform();
        }
    }
}
