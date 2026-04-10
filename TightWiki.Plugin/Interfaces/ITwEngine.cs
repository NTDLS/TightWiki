using Microsoft.Extensions.Logging;
using TightWiki.Plugin.Interfaces.Module;
using TightWiki.Plugin.Interfaces.Module.Function;
using TightWiki.Plugin.Interfaces.Module.Handlers;

namespace TightWiki.Plugin.Interfaces
{
    /// <summary>
    /// Defines the contract for a wiki engine that processes, transforms, and manages wiki content, plugins, and
    /// related components.
    /// </summary>
    /// <remarks>The interface exposes core services and collections required for wiki content processing,
    /// including configuration, plugin management, database access, logging, and extensibility points for content
    /// transformation and exception handling. Implementations are expected to provide thread-safe access to these
    /// members if used concurrently.</remarks>
    public interface ITwEngine
    {
        /// <summary>
        /// Gets the configuration settings for the wiki integration.
        /// </summary>
        TwConfiguration WikiConfiguration { get; }

        /// <summary>
        /// Gets the collection of plugins currently loaded by the application.
        /// </summary>
        /// <remarks>The returned list provides access to all plugins that have been registered and are
        /// available for use. The collection is read-only.</remarks>
        List<ITwPlugin> Plugins { get; }

        /// <summary>
        /// Gets the database manager used to perform database operations.
        /// </summary>
        ITwDatabaseManager DatabaseManager { get; }
        /// <summary>
        /// Gets the logger instance used for logging diagnostic and operational information for the engine.
        /// </summary>
        ILogger<ITwEngine> Logger { get; }

        /// <summary>
        /// Gets the collection of completion handlers associated with the current instance.
        /// </summary>
        List<ITwCompletionDescriptor> CompletionHandlers { get; }
        /// <summary>
        /// Gets the collection of exception handlers associated with this instance.
        /// </summary>
        List<ITwExceptionDescriptor> ExceptionHandlers { get; }
        /// <summary>
        /// Gets the collection of handler descriptors used to process markup elements.
        /// </summary>
        List<ITwHandlerDescriptor> MarkupHandlers { get; }

        /// <summary>
        /// Gets the collection of Standard Function descriptors for loaded plugins.
        /// </summary>
        List<ITwFunctionDescriptor> StandardFunctions { get; }

        /// <summary>
        /// Gets the collection of Scope Function descriptors for loaded plugins.
        /// </summary>
        List<ITwFunctionDescriptor> ScopeFunctions { get; }

        /// <summary>
        /// Gets the collection of Processing Function descriptors for loaded plugins.
        /// </summary>
        List<ITwFunctionDescriptor> ProcessingFunctions { get; }

        /// <summary>
        /// Transforms the content for the given page.
        /// </summary>
        /// <param name="localizer">The localization text provider.</param>
        /// <param name="session">The users current state, used for localization.</param>
        /// <param name="page">The page that is being processed.</param>
        /// <param name="revision">The revision of the page that is being processed.</param>
        /// <param name="omitMatches">The type of matches that we want to omit from processing.</param>
        Task<ITwEngineState> Transform(ITwSharedLocalizationText localizer, ITwSessionState? session, ITwPage page, int? revision = null, TwMatchType[]? omitMatches = null);

        /// <summary>
        /// Transforms the content for the given string markup.
        /// </summary>
        /// <param name="localizer">The localization text provider.</param>
        /// <param name="session">The users current state, used for localization.</param>
        /// <param name="markup">The markup content that is being processed.</param>
        Task<ITwEngineState> Transform(ITwSharedLocalizationText localizer, ITwSessionState? session, string markup);

        /// <summary>
        /// Transforms the content for the given string markup using the lite wiki engine.
        /// </summary>
        /// <param name="localizer">The localization text provider.</param>
        /// <param name="session">The users current state, used for localization.</param>
        /// <param name="markup">The markup content that is being processed.</param>
        Task<ITwEngineState> TransformLite(ITwSharedLocalizationText localizer, ITwSessionState? session, string markup);
    }
}
