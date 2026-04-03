using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Attributes.Functions;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki.Plugin.Default.ProcessingInstructionFunctions
{
    [TwPlugin("Caching & Runtime Behavior", "Built-in processing instruction functions.")]
    public class RuntimeFunctions
    {

        [TwProcessingInstructionFunctionPlugin("NoCache", "Prevents the page from being cached.")]
        public async Task<TwPluginResult> NoCache(ITwEngineState state)
        {
            state.ProcessingInstructions.Add(TwInstruction.NoCache);
            return new TwPluginResult(string.Empty)
            {
                Instructions = [TwResultInstruction.TruncateTrailingLine]
            };
        }
    }
}
