﻿using TightWiki.Engine.Function;
using TightWiki.Engine.Library;
using TightWiki.Engine.Library.Interfaces;
using static TightWiki.Engine.Function.FunctionConstants;
using static TightWiki.Engine.Library.Constants;
using static TightWiki.Library.Constants;

namespace TightWiki.Engine.Implementation.Handlers
{
    /// <summary>
    /// Handles processing-instruction function calls, these functions affect the way the page is processed, but are not directly replaced with text.
    /// </summary>
    public class ProcessingInstructionFunctionHandler : IProcessingInstructionFunctionHandler
    {
        private static FunctionPrototypeCollection? _collection;

        public FunctionPrototypeCollection Prototypes
        {
            get
            {
                if (_collection == null)
                {
                    //---------------------------------------------------------------------------------------------------------
                    // Example function prototypes:                                                                           -
                    //---------------------------------------------------------------------------------------------------------
                    // Function with an optional parameter whose value is constrained to a given set of values:               -
                    //     Example: functionName (parameterType parameterName[allowable,values]='default value')              -
                    //--                                                                                                      -
                    // Function with an optional parameter, which is just a parameter with a default value.                   -
                    //     Example: functionName (parameterType parameterName='default value')                                -
                    //--                                                                                                      -
                    // Function with more than one required parameter:                                                        -
                    //     Example: functionName (parameterType parameterName1, parameterType parameterName2)                 -
                    //--                                                                                                      -
                    // Function with a required parameter and an optional parameter.                                          -
                    // Note that required parameter cannot come after optional parameters.                                    -
                    //     Example: functionName (parameterType parameterName1, parameterType parameterName2='default value') -
                    //--                                                                                                      -
                    // Notes:                                                                                                 -
                    //     Parameter types are defined by the enum: WikiFunctionParamType                                     -
                    //     All values, with the exception of NULL should be enclosed in single-quotes                         -
                    //     The single-quote enclosed escape character is back-slash (e.g. 'John\'s Literal')                  -
                    //--                                                                                                      -
                    //---------------------------------------------------------------------------------------------------------

                    _collection = new FunctionPrototypeCollection(WikiFunctionType.Instruction);
                    _collection.Add("Deprecate ()");
                    _collection.Add("Protect(Boolean isSilent='false')");
                    _collection.Add("Tags (InfiniteString pageTags)");
                    _collection.Add("Template ()");
                    _collection.Add("Review ()");
                    _collection.Add("NoCache ()");
                    _collection.Add("Include ()");
                    _collection.Add("Draft ()");
                    _collection.Add("HideFooterComments ()");
                    _collection.Add("Title(string pageTitle)");
                    _collection.Add("HideFooterLastModified ()");
                }

                return _collection;
            }
        }

        /// <summary>
        /// Called to handle function calls when proper prototypes are matched.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="function">The parsed function call and all its parameters and their values.</param>
        /// <param name="scopeBody">This is not a scope function, this should always be null</param>
        public HandlerResult Handle(ITightEngineState state, FunctionCall function, string? scopeBody = null)
        {
            switch (function.Name.ToLowerInvariant())
            {
                //We check wikifierSession.Factory.CurrentNestLevel here because we don't want to include the processing instructions on any parent pages that are injecting this one.

                //------------------------------------------------------------------------------------------------------------------------------
                //Associates tags with a page. These are saved with the page and can also be displayed.
                case "tags": //##tag(pipe|separated|list|of|tags)
                    {
                        var tags = function.Parameters.GetList<string>("pageTags");
                        state.Tags.AddRange(tags);
                        state.Tags = state.Tags.Distinct().ToList();

                        return new HandlerResult(string.Empty)
                        {
                            Instructions = [HandlerResultInstruction.TruncateTrailingLine]
                        };
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                case "title":
                    {
                        state.PageTitle = function.Parameters.Get<string>("pageTitle");

                        return new HandlerResult(string.Empty)
                        {
                            Instructions = [HandlerResultInstruction.TruncateTrailingLine]
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

                //------------------------------------------------------------------------------------------------------------------------------
                case "protect":
                    {
                        if (state.NestDepth == 0)
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

                //------------------------------------------------------------------------------------------------------------------------------
                case "review":
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

                //------------------------------------------------------------------------------------------------------------------------------
                case "include":
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

                //------------------------------------------------------------------------------------------------------------------------------
                case "draft":
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

            return new HandlerResult() { Instructions = [HandlerResultInstruction.Skip] };
        }
    }
}
