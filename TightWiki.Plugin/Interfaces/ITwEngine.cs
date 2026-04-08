using Microsoft.Extensions.Logging;
using TightWiki.Plugin.Interfaces.Module;
using TightWiki.Plugin.Interfaces.Module.Function;
using TightWiki.Plugin.Interfaces.Module.Handlers;

namespace TightWiki.Plugin.Interfaces
{
    public interface ITwEngine
    {
        TwConfiguration WikiConfiguration { get; }
        List<ITwPlugin> Plugins { get; }

        ITwDatabaseManager DatabaseManager { get; }
        ILogger<ITwEngine> Logger { get; }

        List<ITwCompletionDescriptor> CompletionHandlers { get; }
        List<ITwExceptionDescriptor> ExceptionHandlers { get; }
        List<ITwHandlerDescriptor> MarkupHandlers { get; }

        List<ITwFunctionDescriptor> StandardFunctions { get; }
        List<ITwFunctionDescriptor> ScopeFunctions { get; }
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
