using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Attributes.Functions;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;

namespace TightWiki.Plugin.Default.StandardFunctions
{
    [TwPlugin("Content Composition & Inclusion", "Built-in standard functions.")]
    public class CompositionFunctions
    {
        #region Helpers.

        private static async Task<TwPage?> GetPageFromNavigation(ITwPageRepository pageRepository, string routeData)
        {
            routeData = TwNamespaceNavigation.CleanAndValidate(routeData);
            var page = await pageRepository.GetPageRevisionByNavigation(routeData);
            return page;
        }

        private static void MergeUserVariables(ref ITwEngineState state, Dictionary<string, string> items)
        {
            foreach (var item in items)
            {
                state.Variables[item.Key] = item.Value;
            }
        }

        private static void MergeSnippets(ref ITwEngineState state, Dictionary<string, string> items)
        {
            foreach (var item in items)
            {
                state.Snippets[item.Key] = item.Value;
            }
        }

        #endregion

        [TwStandardFunctionPlugin("Inject", "Injects an un-processed wiki body into the calling page.", isFirstChance: true)]
        public async Task<TwPluginResult> Inject(ITwEngineState state, string pageName)
        {

            var page = await GetPageFromNavigation(state.Engine.DatabaseManager.PageRepository, pageName);
            if (page != null)
            {
                return new TwPluginResult(page.Body)
                {
                    Instructions = [TwResultInstruction.TruncateTrailingLine]
                };
            }
            throw new Exception($"The include page was not found: [{pageName}]");

        }

        [TwStandardFunctionPlugin("Include", "Includes a processed wiki body into the calling page.", isFirstChance: true)]
        public async Task<TwPluginResult> Include(ITwEngineState state, string pageName)
        {
            var page = await GetPageFromNavigation(state.Engine.DatabaseManager.PageRepository, pageName);
            if (page != null)
            {
                var childState = await state.TransformChild(page);

                MergeUserVariables(ref state, childState.Variables);
                MergeSnippets(ref state, childState.Snippets);

                return new TwPluginResult(childState.HtmlResult)
                {
                    Instructions = [TwResultInstruction.TruncateTrailingLine]
                };
            }
            throw new Exception($"The include page was not found: [{pageName}]");
        }

        [TwStandardFunctionPlugin("Snippet", "Displays the value of a snippet.")]
        public async Task<TwPluginResult> Snippet(ITwEngineState state, string name)
        {
            if (state.Snippets.TryGetValue(name, out string? value))
            {
                return new TwPluginResult(value);
            }
            else
            {
                return new TwPluginResult(string.Empty);
            }
        }
    }
}
