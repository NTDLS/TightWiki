using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Attributes.Functions;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;

namespace TightWiki.Plugin.Default.ProcessingInstructionFunctions
{
    [TwPlugin("Page Metadata", "Built-in processing instruction functions.")]
    public class MetadataFunctions
    {
        //Associates tags with a page. These are saved with the page and can also be displayed.
        [TwProcessingInstructionFunctionPlugin("Tags", "Associates tags with a page. These are saved with the page and can also be displayed.")]
        public async Task<TwPluginResult> Tags(ITwEngineState state,
            string[] pageTags)
        {
            state.Tags.AddRange(pageTags);
            state.Tags = state.Tags.Distinct().ToList();

            return new TwPluginResult(string.Empty)
            {
                Instructions = [TwResultInstruction.TruncateTrailingLine]
            };
        }

        [TwProcessingInstructionFunctionPlugin("Title", "Sets the title of the page.")]
        public async Task<TwPluginResult> Title(ITwEngineState state,
            string pageTitle)
        {
            state.PageTitle = pageTitle;

            return new TwPluginResult(string.Empty)
            {
                Instructions = [TwResultInstruction.TruncateTrailingLine]
            };
        }
    }
}
