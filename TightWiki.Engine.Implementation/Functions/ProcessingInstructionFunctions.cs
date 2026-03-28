using TightWiki.Engine.Library;
using TightWiki.Engine.Library.Function.Attributes;
using TightWiki.Engine.Library.Interfaces;
using static TightWiki.Library.Constants;

namespace TightWiki.Engine.Implementation.Functions
{
    [TightWikiFunctionModule("Processing Instruction Functions", "Built-in processing instruction functions.")]
    public class ProcessingInstructionFunctions
        : ITightWikiFunctionModule
    {
        //Associates tags with a page. These are saved with the page and can also be displayed.
        [TightWikiProcessingInstructionFunction("Tags", "Associates tags with a page. These are saved with the page and can also be displayed.")]
        public async Task<HandlerResult> Tags(ITightEngineState state,
            string[] pageTags) //##tag(pipe|separated|list|of|tags)
        {
            state.Tags.AddRange(pageTags);
            state.Tags = state.Tags.Distinct().ToList();

            return new HandlerResult(string.Empty)
            {
                Instructions = [HandlerResultInstruction.TruncateTrailingLine]
            };
        }

        [TightWikiProcessingInstructionFunction("Title", "Sets the title of the page.")]
        public async Task<HandlerResult> Title(ITightEngineState state,
            string pageTitle)
        {
            state.PageTitle = pageTitle;

            return new HandlerResult(string.Empty)
            {
                Instructions = [HandlerResultInstruction.TruncateTrailingLine]
            };
        }

        [TightWikiProcessingInstructionFunction("HideFooterLastModified", "Hides the last modified information in the footer.")]
        public async Task<HandlerResult> HideFooterLastModified(ITightEngineState state)
        {
            state.ProcessingInstructions.Add(WikiInstruction.HideFooterLastModified);

            return new HandlerResult(string.Empty)
            {
                Instructions = [HandlerResultInstruction.TruncateTrailingLine]
            };
        }

        [TightWikiProcessingInstructionFunction("HideFooterComments", "Hides the comments section in the footer.")]
        public async Task<HandlerResult> HideFooterComments(ITightEngineState state)
        {
            state.ProcessingInstructions.Add(WikiInstruction.HideFooterComments);
            return new HandlerResult(string.Empty)
            {
                Instructions = [HandlerResultInstruction.TruncateTrailingLine]
            };
        }

        [TightWikiProcessingInstructionFunction("NoCache", "Prevents the page from being cached.")]
        public async Task<HandlerResult> NoCache(ITightEngineState state)
        {
            state.ProcessingInstructions.Add(WikiInstruction.NoCache);
            return new HandlerResult(string.Empty)
            {
                Instructions = [HandlerResultInstruction.TruncateTrailingLine]
            };
        }

        [TightWikiProcessingInstructionFunction("Deprecate", "Marks the page as deprecated.")]
        public async Task<HandlerResult> Deprecate(ITightEngineState state)
        {
            if (state.NestDepth == 0)
            {
                state.ProcessingInstructions.Add(WikiInstruction.Deprecate);
                state.Headers.Add("<div class=\"alert alert-danger\">This page has been deprecated and will eventually be deleted.</div>");
            }
            return new HandlerResult(string.Empty)
            {
                Instructions = [HandlerResultInstruction.TruncateTrailingLine]
            };
        }

        [TightWikiProcessingInstructionFunction("Protect", "Protects the page from being altered by non-moderators.")]
        public async Task<HandlerResult> Protect(ITightEngineState state, bool isSilent)
        {
            if (state.NestDepth == 0)
            {
                state.ProcessingInstructions.Add(WikiInstruction.Protect);
                if (isSilent == false)
                {
                    state.Headers.Add("<div class=\"alert alert-info\">This page has been protected and can not be changed by non-moderators.</div>");
                }
            }
            return new HandlerResult(string.Empty)
            {
                Instructions = [HandlerResultInstruction.TruncateTrailingLine]
            };
        }

        [TightWikiProcessingInstructionFunction("Template", "Marks the page as a template.")]
        public async Task<HandlerResult> Template(ITightEngineState state)
        {
            if (state.NestDepth == 0)
            {
                state.ProcessingInstructions.Add(WikiInstruction.Template);
                state.Headers.Add("<div class=\"alert alert-secondary\">This page is a template and will not appear in indexes or glossaries.</div>");
            }
            return new HandlerResult(string.Empty)
            {
                Instructions = [HandlerResultInstruction.TruncateTrailingLine]
            };
        }

        [TightWikiProcessingInstructionFunction("Review", "Flags the page for review.")]
        public async Task<HandlerResult> Review(ITightEngineState state)
        {
            if (state.NestDepth == 0)
            {
                state.ProcessingInstructions.Add(WikiInstruction.Review);
                state.Headers.Add("<div class=\"alert alert-warning\">This page has been flagged for review, its content may be inaccurate.</div>");
            }
            return new HandlerResult(string.Empty)
            {
                Instructions = [HandlerResultInstruction.TruncateTrailingLine]
            };
        }

        [TightWikiProcessingInstructionFunction("Include", "Marks the page as an include.")]
        public async Task<HandlerResult> Include(ITightEngineState state)
        {
            if (state.NestDepth == 0)
            {
                state.ProcessingInstructions.Add(WikiInstruction.Include);
                state.Headers.Add("<div class=\"alert alert-secondary\">This page is an include and will not appear in indexes or glossaries.</div>");
            }
            return new HandlerResult(string.Empty)
            {
                Instructions = [HandlerResultInstruction.TruncateTrailingLine]
            };
        }

        [TightWikiProcessingInstructionFunction("Draft", "Marks the page as a draft.")]
        public async Task<HandlerResult> Draft(ITightEngineState state)
        {
            if (state.NestDepth == 0)
            {
                state.ProcessingInstructions.Add(WikiInstruction.Draft);
                state.Headers.Add("<div class=\"alert alert-warning\">This page is a draft and may contain incorrect information and/or experimental styling.</div>");
            }
            return new HandlerResult(string.Empty)
            {
                Instructions = [HandlerResultInstruction.TruncateTrailingLine]
            };
        }
    }
}
