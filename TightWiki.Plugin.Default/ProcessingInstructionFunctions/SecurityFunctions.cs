using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Attributes.Functions;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki.Plugin.Default.ProcessingInstructionFunctions
{
    [TwPlugin("Security & Access Control", "Built-in processing instruction functions.")]
    public class SecurityFunctions
    {
        [TwProcessingInstructionFunctionPlugin("Protect", "Protects the page from being altered by non-moderators.")]
        public async Task<TwPluginResult> Protect(ITwEngineState state, bool isSilent)
        {
            if (state.NestDepth == 0)
            {
                state.ProcessingInstructions.Add(TwInstruction.Protect);
                if (isSilent == false)
                {
                    state.Headers.Add("<div class=\"alert alert-info\">This page has been protected and can not be changed by non-moderators.</div>");
                }
            }
            return new TwPluginResult(string.Empty)
            {
                Instructions = [TwResultInstruction.TruncateTrailingLine]
            };
        }
    }
}
