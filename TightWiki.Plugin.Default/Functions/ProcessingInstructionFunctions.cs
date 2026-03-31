using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Attributes.Functions;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki.Plugin.Default.Functions
{
    [TwPlugin("Processing Instructions", "Built-in processing instruction functions.", 1000)]
    public class ProcessingInstructionFunctions
    {
        //Associates tags with a page. These are saved with the page and can also be displayed.
        [TwProcessingInstructionFunctionPlugin("Tags", "Associates tags with a page. These are saved with the page and can also be displayed.", 1000)]
        public async Task<TwPluginResult> Tags(ITwEngineState state,
            string[] pageTags) //##tag(pipe|separated|list|of|tags)
        {
            state.Tags.AddRange(pageTags);
            state.Tags = state.Tags.Distinct().ToList();

            return new TwPluginResult(string.Empty)
            {
                Instructions = [TwResultInstruction.TruncateTrailingLine]
            };
        }

        [TwProcessingInstructionFunctionPlugin("Title", "Sets the title of the page.", 1000)]
        public async Task<TwPluginResult> Title(ITwEngineState state,
            string pageTitle)
        {
            state.PageTitle = pageTitle;

            return new TwPluginResult(string.Empty)
            {
                Instructions = [TwResultInstruction.TruncateTrailingLine]
            };
        }

        [TwProcessingInstructionFunctionPlugin("HideFooterLastModified", "Hides the last modified information in the footer.", 1000)]
        public async Task<TwPluginResult> HideFooterLastModified(ITwEngineState state)
        {
            state.ProcessingInstructions.Add(TwInstruction.HideFooterLastModified);

            return new TwPluginResult(string.Empty)
            {
                Instructions = [TwResultInstruction.TruncateTrailingLine]
            };
        }

        [TwProcessingInstructionFunctionPlugin("HideFooterComments", "Hides the comments section in the footer.", 1000)]
        public async Task<TwPluginResult> HideFooterComments(ITwEngineState state)
        {
            state.ProcessingInstructions.Add(TwInstruction.HideFooterComments);
            return new TwPluginResult(string.Empty)
            {
                Instructions = [TwResultInstruction.TruncateTrailingLine]
            };
        }

        [TwProcessingInstructionFunctionPlugin("NoCache", "Prevents the page from being cached.", 1000)]
        public async Task<TwPluginResult> NoCache(ITwEngineState state)
        {
            state.ProcessingInstructions.Add(TwInstruction.NoCache);
            return new TwPluginResult(string.Empty)
            {
                Instructions = [TwResultInstruction.TruncateTrailingLine]
            };
        }

        [TwProcessingInstructionFunctionPlugin("Deprecate", "Marks the page as deprecated.", 1000)]
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

        [TwProcessingInstructionFunctionPlugin("Protect", "Protects the page from being altered by non-moderators.", 1000)]
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

        [TwProcessingInstructionFunctionPlugin("Template", "Marks the page as a template.", 1000)]
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

        [TwProcessingInstructionFunctionPlugin("Review", "Flags the page for review.", 1000)]
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

        [TwProcessingInstructionFunctionPlugin("Include", "Marks the page as an include.", 1000)]
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

        [TwProcessingInstructionFunctionPlugin("Draft", "Marks the page as a draft.", 1000)]
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
