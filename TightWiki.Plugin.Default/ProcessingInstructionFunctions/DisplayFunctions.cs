using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Attributes.Functions;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki.Plugin.Default.ProcessingInstructionFunctions
{
    [TwPlugin("Display Controls", "Built-in processing instruction functions.")]
    public class DisplayFunctions
    {

        [TwProcessingInstructionFunctionPlugin("HideFooterLastModified", "Hides the last modified information in the footer.")]
        public async Task<TwPluginResult> HideFooterLastModified(ITwEngineState state)
        {
            state.ProcessingInstructions.Add(TwInstruction.HideFooterLastModified);

            return new TwPluginResult(string.Empty)
            {
                Instructions = [TwResultInstruction.TruncateTrailingLine]
            };
        }

        [TwProcessingInstructionFunctionPlugin("HideFooterComments", "Hides the comments section in the footer.")]
        public async Task<TwPluginResult> HideFooterComments(ITwEngineState state)
        {
            state.ProcessingInstructions.Add(TwInstruction.HideFooterComments);
            return new TwPluginResult(string.Empty)
            {
                Instructions = [TwResultInstruction.TruncateTrailingLine]
            };
        }

    }
}
