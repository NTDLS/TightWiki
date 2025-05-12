﻿using System.Text;
using TightWiki.Engine.Function;
using TightWiki.Engine.Implementation.Utility;
using TightWiki.Engine.Library;
using TightWiki.Engine.Library.Interfaces;
using TightWiki.Models;
using static TightWiki.Engine.Function.FunctionConstants;
using static TightWiki.Engine.Library.Constants;

namespace TightWiki.Engine.Implementation.Handlers
{
    /// <summary>
    /// Handles post-processing function calls.
    /// </summary>
    public class PostProcessingFunctionHandler : IPostProcessingFunctionHandler
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

                    _collection = new FunctionPrototypeCollection(WikiFunctionType.Standard);
                    _collection.Add("Tags (string styleName['Flat','List']='List')");
                    _collection.Add("TagCloud (string pageTag, integer Top='1000')");
                    _collection.Add("SearchCloud (string searchPhrase, integer top ='1000')");
                    _collection.Add("TOC( boolean alphabetized='false')");
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
                //------------------------------------------------------------------------------------------------------------------------------
                //Displays a tag link list.
                case "tags": //##tags
                    {
                        string styleName = function.Parameters.Get<string>("styleName").ToLowerInvariant();
                        var html = new StringBuilder();

                        if (styleName == "list")
                        {
                            html.Append("<ul>");
                            foreach (var tag in state.Tags)
                            {
                                html.Append($"<li><a href=\"{GlobalConfiguration.BasePath}/Tag/Browse/{tag}\">{tag}</a>");
                            }
                            html.Append("</ul>");
                        }
                        else if (styleName == "flat")
                        {
                            foreach (var tag in state.Tags)
                            {
                                if (html.Length > 0) html.Append(" | ");
                                html.Append($"<a href=\"{GlobalConfiguration.BasePath}/Tag/Browse/{tag}\">{tag}</a>");
                            }
                        }

                        return new HandlerResult(html.ToString());
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                case "tagcloud":
                    {
                        var top = function.Parameters.Get<int>("Top");
                        string seedTag = function.Parameters.Get<string>("pageTag");

                        string html = TagCloud.Build(seedTag, top);
                        return new HandlerResult(html);
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                case "searchcloud":
                    {
                        var top = function.Parameters.Get<int>("Top");
                        var tokens = function.Parameters.Get<string>("searchPhrase").Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList();

                        string html = SearchCloud.Build(tokens, top);
                        return new HandlerResult(html);
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                //Displays a table of contents for the page based on the header tags.
                case "toc":
                    {
                        bool alphabetized = function.Parameters.Get<bool>("alphabetized");

                        var html = new StringBuilder();

                        var tags = (from t in state.TableOfContents
                                    orderby t.StartingPosition
                                    select t).ToList();

                        var unordered = new List<TableOfContentsTag>();
                        var ordered = new List<TableOfContentsTag>();

                        if (alphabetized)
                        {
                            int level = tags.FirstOrDefault()?.Level ?? 0;

                            foreach (var tag in tags)
                            {
                                if (level != tag.Level)
                                {
                                    ordered.AddRange(unordered.OrderBy(o => o.Text));
                                    unordered.Clear();
                                    level = tag.Level;
                                }

                                unordered.Add(tag);
                            }

                            ordered.AddRange(unordered.OrderBy(o => o.Text));
                            unordered.Clear();

                            tags = ordered.ToList();
                        }

                        int currentLevel = 0;

                        foreach (var tag in tags)
                        {
                            if (tag.Level > currentLevel)
                            {
                                while (currentLevel < tag.Level)
                                {
                                    html.Append("<ul>");
                                    currentLevel++;
                                }
                            }
                            else if (tag.Level < currentLevel)
                            {
                                while (currentLevel > tag.Level)
                                {

                                    html.Append("</ul>");
                                    currentLevel--;
                                }
                            }

                            html.Append("<li><a href=\"#" + tag.HrefTag + "\">" + tag.Text + "</a></li>");
                        }

                        while (currentLevel > 0)
                        {
                            html.Append("</ul>");
                            currentLevel--;
                        }

                        return new HandlerResult(html.ToString());
                    }
            }

            return new HandlerResult() { Instructions = [HandlerResultInstruction.Skip] };
        }
    }
}