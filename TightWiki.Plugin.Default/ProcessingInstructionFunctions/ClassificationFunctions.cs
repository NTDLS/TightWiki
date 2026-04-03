using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Attributes.Functions;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki.Plugin.Default.ProcessingInstructionFunctions
{
    [TwPlugin("Page Classification & Editorial State", "Built-in processing instruction functions.")]
    public class ClassificationFunctions
    {
        [TwProcessingInstructionFunctionPlugin("Deprecate", "Marks the page as deprecated.")]
        public async Task<TwPluginResult> Deprecate(ITwEngineState state)
        {
            if (state.NestDepth == 0)
            {
                state.ProcessingInstructions.Add(TwInstruction.Deprecate);
                state.Headers.Add("<div class=\"alert alert-danger\">This page has been deprecated and will eventually be deleted.</div>");
            }
            return new TwPluginResult(string.Empty)
            {
                Instructions = [TwResultInstruction.TruncateTrailingLine]
            };
        }

        [TwProcessingInstructionFunctionPlugin("Template", "Marks the page as a template.")]
        public async Task<TwPluginResult> Template(ITwEngineState state)
        {
            if (state.NestDepth == 0)
            {
                state.ProcessingInstructions.Add(TwInstruction.Template);
                state.Headers.Add("<div class=\"alert alert-secondary\">This page is a template and will not appear in indexes or glossaries.</div>");
            }
            return new TwPluginResult(string.Empty)
            {
                Instructions = [TwResultInstruction.TruncateTrailingLine]
            };
        }

        [TwProcessingInstructionFunctionPlugin("Review", "Flags the page for review.")]
        public async Task<TwPluginResult> Review(ITwEngineState state)
        {
            if (state.NestDepth == 0)
            {
                state.ProcessingInstructions.Add(TwInstruction.Review);
                state.Headers.Add("<div class=\"alert alert-warning\">This page has been flagged for review, its content may be inaccurate.</div>");
            }
            return new TwPluginResult(string.Empty)
            {
                Instructions = [TwResultInstruction.TruncateTrailingLine]
            };
        }

        [TwProcessingInstructionFunctionPlugin("Include", "Marks the page as an include.")]
        public async Task<TwPluginResult> Include(ITwEngineState state)
        {
            if (state.NestDepth == 0)
            {
                state.ProcessingInstructions.Add(TwInstruction.Include);
                state.Headers.Add("<div class=\"alert alert-secondary\">This page is an include and will not appear in indexes or glossaries.</div>");
            }
            return new TwPluginResult(string.Empty)
            {
                Instructions = [TwResultInstruction.TruncateTrailingLine]
            };
        }

        [TwProcessingInstructionFunctionPlugin("Draft", "Marks the page as a draft.")]
        public async Task<TwPluginResult> Draft(ITwEngineState state)
        {
            if (state.NestDepth == 0)
            {
                state.ProcessingInstructions.Add(TwInstruction.Draft);
                state.Headers.Add("<div class=\"alert alert-warning\">This page is a draft and may contain incorrect information and/or experimental styling.</div>");
            }
            return new TwPluginResult(string.Empty)
            {
                Instructions = [TwResultInstruction.TruncateTrailingLine]
            };
        }
    }
}
