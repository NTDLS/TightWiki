using System.Text;
using TightWiki.Engine.Library;
using TightWiki.Engine.Library.Interfaces;
using TightWiki.EngineFunction;
using TightWiki.Repository;
using static TightWiki.Engine.Library.Constants;
using static TightWiki.EngineFunction.FunctionPrototypeCollection;
using static TightWiki.Library.Constants;

namespace TightWiki.Engine.Handlers
{
    public class ProcessingInstructionFunctionHandler : IProcessingInstructionFunctionHandler
    {
        private static FunctionPrototypeCollection? _collection;

        public FunctionPrototypeCollection Prototypes
        {
            get
            {
                if (_collection == null)
                {
                    _collection = new FunctionPrototypeCollection(WikiFunctionType.Instruction);

                    #region Prototypes.

                    //Processing instructions:
                    _collection.Add("@@Deprecate:");
                    _collection.Add("@@Protect:<bool>{isSilent}='false'");
                    _collection.Add("@@Template:");
                    _collection.Add("@@Review:");
                    _collection.Add("@@NoCache:");
                    _collection.Add("@@Include:");
                    _collection.Add("@@Draft:");
                    _collection.Add("@@HideFooterComments:");
                    _collection.Add("@@HideFooterLastModified:");

                    //System functions:
                    _collection.Add("@@SystemEmojiCategoryList:");
                    _collection.Add("@@SystemEmojiList:");
                    #endregion
                }

                return _collection;
            }
        }

        public HandlerResult Handle(ITightEngineState state, FunctionCall function, string scopeBody)
        {
            switch (function.Name.ToLower())
            {
                //We check wikifierSession.Factory.CurrentNestLevel here because we don't want to include the processing instructions on any parent pages that are injecting this one.

                //------------------------------------------------------------------------------------------------------------------------------
                case "systememojilist":
                    {
                        StringBuilder html = new();

                        html.Append($"<table class=\"table table-striped table-bordered \">");

                        html.Append($"<thead>");
                        html.Append($"<tr>");
                        html.Append($"<td><strong>Name</strong></td>");
                        html.Append($"<td><strong>Image</strong></td>");
                        html.Append($"<td><strong>Shortcut</strong></td>");
                        html.Append($"</tr>");
                        html.Append($"</thead>");

                        string category = state.QueryString["Category"].ToString();

                        html.Append($"<tbody>");

                        if (string.IsNullOrWhiteSpace(category) == false)
                        {
                            var emojis = EmojiRepository.GetEmojisByCategory(category);

                            foreach (var emoji in emojis)
                            {
                                html.Append($"<tr>");
                                html.Append($"<td>{emoji.Name}</td>");
                                //html.Append($"<td><img src=\"/images/emoji/{emoji.Path}\" /></td>");
                                html.Append($"<td><img src=\"/File/Emoji/{emoji.Name.ToLower()}\" /></td>");
                                html.Append($"<td>{emoji.Shortcut}</td>");
                                html.Append($"</tr>");
                            }
                        }

                        html.Append($"</tbody>");
                        html.Append($"</table>");

                        return new HandlerResult(html.ToString());
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                case "systememojicategorylist":
                    {
                        var categories = EmojiRepository.GetEmojiCategoriesGrouped();

                        StringBuilder html = new();

                        html.Append($"<table class=\"table table-striped table-bordered \">");

                        int rowNumber = 0;

                        html.Append($"<thead>");
                        html.Append($"<tr>");
                        html.Append($"<td><strong>Name</strong></td>");
                        html.Append($"<td><strong>Count of Emojis</strong></td>");
                        html.Append($"</tr>");
                        html.Append($"</thead>");

                        foreach (var category in categories)
                        {
                            if (rowNumber == 1)
                            {
                                html.Append($"<tbody>");
                            }

                            html.Append($"<tr>");
                            html.Append($"<td><a href=\"/wiki_help::list_of_emojis_by_category?category={category.Category}\">{category.Category}</a></td>");
                            html.Append($"<td>{category.EmojiCount:N0}</td>");
                            html.Append($"</tr>");
                            rowNumber++;
                        }

                        html.Append($"</tbody>");
                        html.Append($"</table>");

                        return new HandlerResult(html.ToString())
                        {
                            Instructions = [HandlerResultInstruction.DisallowNestedProcessing]
                        };
                    }
                //------------------------------------------------------------------------------------------------------------------------------
                case "hidefooterlastmodified":
                    {
                        state.ProcessingInstructions.Add(WikiInstruction.HideFooterLastModified);

                        return new HandlerResult(string.Empty)
                        {
                            Instructions = [HandlerResultInstruction.TruncateTrailingLine]
                        };
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                case "hidefootercomments":
                    {
                        state.ProcessingInstructions.Add(WikiInstruction.HideFooterComments);
                        return new HandlerResult(string.Empty)
                        {
                            Instructions = [HandlerResultInstruction.TruncateTrailingLine]
                        };
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                case "nocache":
                    {
                        state.ProcessingInstructions.Add(WikiInstruction.NoCache);
                        return new HandlerResult(string.Empty)
                        {
                            Instructions = [HandlerResultInstruction.TruncateTrailingLine]
                        };
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                case "deprecate":
                    {
                        if (state.Engine.CurrentNestLevel == 0)
                        {
                            state.ProcessingInstructions.Add(WikiInstruction.Deprecate);
                            state.Headers.Add("<div class=\"alert alert-danger\">This page has been deprecated and will eventually be deleted.</div>");
                        }
                        return new HandlerResult(string.Empty)
                        {
                            Instructions = [HandlerResultInstruction.TruncateTrailingLine]
                        };
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                case "protect":
                    {
                        if (state.Engine.CurrentNestLevel == 0)
                        {
                            bool isSilent = function.Parameters.Get<bool>("isSilent");
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

                //------------------------------------------------------------------------------------------------------------------------------
                case "template":
                    {
                        if (state.Engine.CurrentNestLevel == 0)
                        {
                            state.ProcessingInstructions.Add(WikiInstruction.Template);
                            state.Headers.Add("<div class=\"alert alert-secondary\">This page is a template and will not appear in indexes or glossaries.</div>");
                        }
                        return new HandlerResult(string.Empty)
                        {
                            Instructions = [HandlerResultInstruction.TruncateTrailingLine]
                        };
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                case "review":
                    {
                        if (state.Engine.CurrentNestLevel == 0)
                        {
                            state.ProcessingInstructions.Add(WikiInstruction.Review);
                            state.Headers.Add("<div class=\"alert alert-warning\">This page has been flagged for review, its content may be inaccurate.</div>");
                        }
                        return new HandlerResult(string.Empty)
                        {
                            Instructions = [HandlerResultInstruction.TruncateTrailingLine]
                        };
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                case "include":
                    {
                        if (state.Engine.CurrentNestLevel == 0)
                        {
                            state.ProcessingInstructions.Add(WikiInstruction.Include);
                            state.Headers.Add("<div class=\"alert alert-secondary\">This page is an include and will not appear in indexes or glossaries.</div>");
                        }
                        return new HandlerResult(string.Empty)
                        {
                            Instructions = [HandlerResultInstruction.TruncateTrailingLine]
                        };
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                case "draft":
                    {
                        if (state.Engine.CurrentNestLevel == 0)
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

            return new HandlerResult() { Instructions = [HandlerResultInstruction.Skip] };
        }
    }
}
