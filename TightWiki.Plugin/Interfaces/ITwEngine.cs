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

        List<ITwCompletionPlugin> CompletionHandlers { get; }
        List<ITwExceptionPlugin> ExceptionHandlers { get; }
        List<ITwHandlerDescriptor> MarkupHandlers { get; }

        List<ITwFunctionDescriptor> StandardFunctions { get; }
        List<ITwFunctionDescriptor> ScopeFunctions { get; }
        List<ITwFunctionDescriptor> ProcessingFunctions { get; }

        Task<ITwEngineState> Transform(ITwSharedLocalizationText localizer, ITwSessionState? sessionState, ITwPage page, int? revision = null, TwMatchType[]? omitMatches = null);
        Task<ITwEngineState> Transform(ITwSharedLocalizationText localizer, ITwSessionState? session, string markup);
    }
}
