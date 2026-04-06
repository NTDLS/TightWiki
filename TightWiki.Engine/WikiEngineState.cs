using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using TightWiki.Engine.Module.Function;
using TightWiki.Library;
using TightWiki.Library.Security;
using TightWiki.Plugin;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Models;

namespace TightWiki.Engine
{
    public class WikiEngineState
        : ITwEngineState
    {
        public ITwEngine Engine { get; private set; }
        public ITwSharedLocalizationText Localizer { get; private set; }

        private string _queryTokenHash = "c03a1c9e-da83-479b-87e8-21d7906bd866";
        private int _matchesStoredPerIteration = 0;
        private readonly string _tocName = "TOC_" + new Random().Next(0, 1000000).ToString();
        private readonly Dictionary<string, object> _handlerState = new();

        #region Public properties.

        /// <summary>
        /// Custom page title set by a call to @@Title("...")
        /// </summary>
        public string? PageTitle { get; set; }
        public int ErrorCount { get; private set; }
        public int MatchCount { get; private set; }
        public int TransformIterations { get; private set; }
        public TimeSpan ProcessingTime { get; private set; }
        public Dictionary<string, string> Variables { get; } = new();
        public Dictionary<string, string> Snippets { get; } = new();
        public List<TwPageReference> OutgoingLinks { get; private set; } = new();
        public List<string> ProcessingInstructions { get; private set; } = new();
        public string HtmlResult { get; private set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
        public Dictionary<string, MatchSet> Matches { get; private set; } = new();
        public List<TwTableOfContentsTag> TableOfContents { get; } = new();
        public List<string> Headers { get; } = new();

        #endregion

        #region Input parameters.

        public ITwPage Page { get; }
        public int? Revision { get; }
        public IQueryCollection QueryString { get; }
        public ITwSessionState? Session { get; }
        public HashSet<TwMatchType> OmitMatches { get; private set; } = new();
        public int NestDepth { get; private set; } //Used for recursion.
        public ILogger<ITwEngine> Logger { get; private set; }

        #endregion

        private static string NewIdentiferTag()
            => $"{Constants.TagStart}{Guid.NewGuid():N}{Constants.TagEnd}";

        /// <summary>
        /// Used to store values for handlers that needs to survive only a single wiki processing session.
        /// </summary>
        public void SetStateValue<T>(string key, T value)
        {
            if (value == null)
            {
                return;
            }
            _handlerState[key] = value;
        }

        /// <summary>
        /// Used to get values for handlers that needs to survive only a single wiki processing session.
        /// </summary>
        public T GetStateValue<T>(string key, T defaultValue)
        {
            if (_handlerState.TryGetValue(key, out var value))
            {
                return (T)value;
            }
            SetStateValue(key, defaultValue);
            return defaultValue;
        }

        /// <summary>
        /// Used to get values for handlers that needs to survive only a single wiki processing session.
        /// </summary>
        public bool TryGetStateValue<T>(string key, [MaybeNullWhen(false)] out T? outValue)
        {
            if (_handlerState.TryGetValue(key, out var value))
            {
                outValue = (T)value;
                return true;
            }

            outValue = default;
            return false;
        }

        /// <summary>
        /// Creates a new instance of the TightEngineState class. Typically created by a call to TightEngine.Transform().
        /// </summary>
        /// <param name="session">The users current state, used for localization.</param>
        /// <param name="page">The page that is being processed.</param>
        /// <param name="revision">The revision of the page that is being processed.</param>
        /// <param name="omitMatches">The type of matches that we want to omit from processing.</param>
        /// <param name="nestDepth">The current depth of recursion.</param>
        internal WikiEngineState(ILogger<ITwEngine> logger, ITwEngine engine, ITwSharedLocalizationText localizer, ITwSessionState? session,
            ITwPage page, int? revision = null, TwMatchType[]? omitMatches = null, int nestDepth = 0)
        {
            Localizer = localizer;
            Logger = logger;
            QueryString = session?.QueryString ?? new EmptyQueryCollection();
            Page = page;
            Revision = revision;
            Matches = new Dictionary<string, MatchSet>();
            Session = session;
            NestDepth = nestDepth;

            if (omitMatches != null)
            {
                OmitMatches.UnionWith(omitMatches);
            }

            Engine = engine;
        }

        /// <summary>
        /// Transforms "included" wiki pages, for example if a wiki function
        /// injected additional wiki markup, this 'could' be processed separately.
        /// </summary>
        /// <param name="page">The child page to process.</param>
        /// <param name="revision">The optional revision of the child page to process.</param>
        public async Task<ITwEngineState> TransformChild(ITwPage page, int? revision = null)
        {
            var childState = new WikiEngineState(Logger, Engine, Localizer, Session, page, revision, OmitMatches.ToArray(), NestDepth + 1);

            return await childState.Transform();
        }

        internal async Task<ITwEngineState> Transform()
        {
            var startTime = DateTime.UtcNow;

            try
            {
                var pageContent = new TwString(Page.Body);

                pageContent.Replace("\r\n", "\n");

                //The order of transformations is important, the first thing we need to do is transform
                //  literals so that the content inside them is not transformed by any other handlers
                //  and is displayed verbatim on the page. For example, if we have a code block with
                //  some wiki markup inside it, we don't want that wiki markup to be transformed,
                //  we want it to be displayed as-is.
                TransformLiterals(pageContent);

                //Now we transform all other handlers, we loop through them until we have no more matches
                //  to transform because some handlers can introduce new wiki markup that needs to be
                //  transformed. For example, if we have a template that includes other templates, we
                //  need to keep transforming until we have transformed all the included templates and
                //  there is no more wiki markup to transform.
                while ((await TransformAll(pageContent)) > 0)
                {
                }

                //Now we transform post-processing functions, these are functions that need to be
                //  transformed after all other transformations have been done. For example, we
                //  can't build a table-of-contents until we have parsed the entire page and
                //  identified all the headings.
                await TransformPostProcessingFunctions(pageContent);
                TransformNewlines(pageContent);

                if (PageTitle != null)
                {
                    //Page title will be a wiki placeholder (set by the title() function), retrieve the content and replace the title.
                    if (Matches.TryGetValue(PageTitle, out var pageTitle))
                    {
                        PageTitle = pageTitle.Content;
                    }
                }

                //Finally, we swap in all the matches that we have stored throughout
                //  the transformation process with their resulting content.
                SwapInStoredMatches(pageContent, true);

                //While we were transforming, we replaced new-lines with magic strings so now we need
                //  to swap those back to real new-lines and line-breaks for the final HTML result.
                SwapInLineBreaks(pageContent);

                //Prepend any headers that were added by wiki handlers.
                foreach (var header in Headers)
                {
                    pageContent.Insert(0, header);
                }

                //TADA! We should have our final HTML result ready to go.
                HtmlResult = pageContent.ToString();
            }
            catch (Exception ex)
            {
                await StoreCriticalWikiError(ex);
            }

            ProcessingTime = DateTime.UtcNow - startTime;

            foreach (var handler in Engine.CompletionHandlers)
            {
                await handler.Handle(this);
            }

            return this;
        }

        private async Task<int> TransformAll(TwString pageContent)
        {
            TransformIterations++;

            _matchesStoredPerIteration = 0;

            //Some of the functions need to be processed before others, for example the CODE function
            //  needs to be processed before other functions because it can contain wiki markup that
            //  we don't want to transform, we want it to be displayed verbatim on the page. So we do
            //  a first pass for those functions, then we do a second pass for the rest of the functions.
            await TransformScopeFunctions(pageContent, true); //First pass for "first chance" functions.
            await TransformStandardFunctions(pageContent, true); //First pass for "first chance" functions.

            await TransformComments(pageContent);
            await TransformHeadings(pageContent);

            await TransformScopeFunctions(pageContent, false); //Second pass for all functions.
            await TransformVariables(pageContent);
            await TransformLinks(pageContent);
            await TransformMarkup(pageContent);
            await TransformEmoji(pageContent);

            await TransformStandardFunctions(pageContent, false); //Second pass for all functions.
            await TransformProcessingInstructionFunctions(pageContent);

            //Lastly, we swap in all the matches that we have stored throughout this iteration with
            //  their resulting content so that they can be transformed in the next iteration if needed.
            //  We have to do this at the end of the iteration because if we swap them in before we have
            //  transformed all the handlers, we might end up with new wiki markup that needs to be
            //  transformed in this same iteration and we would miss it if we swapped them in before
            //  transforming all the handlers.
            SwapInStoredMatches(pageContent, false);

            return _matchesStoredPerIteration;
        }

        /// <summary>
        /// Replaces placeholders in the specified page content with previously stored match values.
        /// </summary>
        /// <param name="pageContent">The page content in which placeholders will be replaced. Cannot be null.</param>
        /// <param name="forceNestedDecode">If true, matches are replaced even if they are set to not allow nested decode.
        /// ForceDecode is typically only executed at the end of all processing but is made available here for special use cases by custom functions.
        /// <see cref="MatchSet.AllowNestedDecode"/></param>
        public void SwapInStoredMatches(TwString pageContent, bool forceNestedDecode)
        {
            //We have to replace a few times because we could have replace tags (guids) nested inside others.
            int length;
            do
            {
                length = pageContent.Length;
                foreach (var v in Matches)
                {
                    if (v.Value.AllowNestedDecode || forceNestedDecode)
                    {
                        if (OmitMatches.Contains(v.Value.MatchType))
                        {
                            /// When matches are omitted, the entire match will be removed from the resulting wiki text.
                            pageContent.Replace(v.Key, string.Empty);
                        }
                        else
                        {
                            pageContent.Replace(v.Key, v.Value.Content);
                        }

                    }
                }
            } while (length != pageContent.Length);
        }

        /// <summary>
        /// Replaces soft and hard line break markers in the specified page content with the provided override value or
        /// a default replacement.
        /// </summary>
        /// <remarks>This method modifies the provided WikiString instance in place. If different
        /// replacements are needed for soft and hard breaks, call this method separately for each type.</remarks>
        /// <param name="pageContent">The wiki page content in which line break markers will be replaced. Cannot be null.</param>
        /// <param name="overrideValue">The string to use as a replacement for both soft and hard line break markers. If null, uses "\r\n" for soft
        /// breaks and "<br />" for hard breaks.</param>
        public void SwapInLineBreaks(TwString pageContent, string? overrideValue = null)
        {
            pageContent.Replace(Constants.SoftBreak, overrideValue ?? "\r\n");
            pageContent.Replace(Constants.HardBreak, overrideValue ?? "<br />");
        }

        /// <summary>
        /// Transform basic markup such as bold, italics, underline, etc. for single and multi-line.
        /// </summary>
        private async Task TransformMarkup(TwString pageContent)
        {
            var symbols = WikiUtility.GetApplicableSymbols(pageContent.ToString());

            foreach (var symbol in symbols)
            {
                var sequence = new string(symbol, 2);
                var escapedSequence = Regex.Escape(sequence);

                var rgx = new Regex(@$"{escapedSequence}(.*?){escapedSequence}", RegexOptions.IgnoreCase);
                var orderedMatches = WikiUtility.OrderMatchesByLengthDescending(rgx.Matches(pageContent.ToString()));
                foreach (var match in orderedMatches)
                {
                    string body = match.Value.Substring(sequence.Length, match.Value.Length - sequence.Length * 2);

                    bool wasHandled = false;
                    foreach (var handler in Engine.MarkupHandlers)
                    {
                        wasHandled = true;
                        var result = await handler.Handle(this, symbol, body);
                        if (!result.Instructions.Contains(TwResultInstruction.Skip))
                        {
                            StoreHandlerResult(result, TwMatchType.Markup, pageContent, match.Value);
                            break;
                        }
                    }
                    if (!wasHandled)
                    {
                        await StoreWikiError(pageContent, match.Value, $"No markup tag handler processed the instruction: \"{match.Value}\".");
                    }
                }
            }

            var sizeUpOrderedMatches = WikiUtility.OrderMatchesByLengthDescending(
                PrecompiledRegex.TransformHeaderMarkup().Matches(pageContent.ToString()));

            foreach (var match in sizeUpOrderedMatches)
            {
                int headingMarkers = 0;
                foreach (char c in match.Value)
                {
                    if (c != '^')
                    {
                        break;
                    }
                    headingMarkers++;
                }
                if (headingMarkers >= 2)
                {
                    string value = match.Value.Substring(headingMarkers).Trim();
                    double fontSize = 2.2 - (7 - headingMarkers) * 0.2;
                    string markup = $"<span class=\"mb-0\" style=\"font-size: {fontSize}rem;\">{value}</span>\r\n";
                    StoreMatch(TwMatchType.Markup, pageContent, match.Value, markup);
                }
            }
        }

        /// <summary>
        /// Transform inline and multi-line literal blocks. These are blocks where the content
        /// will not be wikified and contain code that is encoded to display verbatim on the page.
        /// </summary>
        private void TransformLiterals(TwString pageContent)
        {
            //TODO: May need to do the same thing we did with TransformBlocks() to match all these if they need to be nested.

            //Transform literal strings, even encodes HTML so that it displays verbatim.
            var orderedMatches = WikiUtility.OrderMatchesByLengthDescending(
                PrecompiledRegex.TransformLiterals().Matches(pageContent.ToString()));

            foreach (var match in orderedMatches)
            {
                string value = match.Value.Substring(2, match.Value.Length - 4);
                value = HttpUtility.HtmlEncode(value);
                StoreMatch(TwMatchType.Literal, pageContent, match.Value, value.Replace("\r", "").Trim().Replace("\n", $"{Constants.HardBreak}"), false);
            }
        }

        /// <summary>
        /// Matching nested blocks with regex was hell, I escaped with a loop. ¯\_(ツ)_/¯
        /// </summary>
        private async Task TransformScopeFunctions(TwString pageContent, bool onlyProcessFirstChanceFunctions)
        {
            var content = pageContent.ToString();

            string rawBlock = string.Empty;

            int startPos = content.Length - 1;

            while (true)
            {
                startPos = content.LastIndexOf("{{", startPos, StringComparison.InvariantCultureIgnoreCase);
                if (startPos < 0)
                {
                    break;
                }
                int enterScope;
                int exitScope;
                int endScopeSearchIndex = startPos;

                do
                {
                    int endPos = content.IndexOf("}}", endScopeSearchIndex, StringComparison.InvariantCultureIgnoreCase);

                    if (endPos < 0 || endPos <= startPos)
                    {
                        var exception = new StringBuilder();
                        exception.AppendLine($"<strong>A parsing error occurred after position {startPos}:<br /></strong> Unable to locate closing tag.<br /><br />");
                        if (rawBlock?.Length > 0)
                        {
                            exception.AppendLine($"<strong>The last successfully parsed block was:</strong><br /> {rawBlock}");
                        }
                        exception.AppendLine($"<strong>The problem occurred after:</strong><br /> {pageContent.ToString().Substring(startPos)}<br /><br />");
                        exception.AppendLine($"<strong>The content the parser was working on is:</strong><br /> {content}<br /><br />");

                        throw new Exception(exception.ToString());
                    }

                    rawBlock = content.Substring(startPos, endPos - startPos + 2);

                    //Count the number of opening and closing tags in the block to make sure
                    //  we are matching the correct closing tag for this block, not a nested one.
                    //If there are more opening tags than closing tags, we know that the closing
                    //  tag we found is for a nested block and not the current block, so we keep
                    //  searching until we find the closing tag that matches the number of opening
                    //  tags (or we run out of content to search through, which would be an error).
                    enterScope = Utility.CountOccurrencesOf(rawBlock, "{{");
                    exitScope = Utility.CountOccurrencesOf(rawBlock, "}}");

                    endScopeSearchIndex++;
                } while (enterScope > exitScope);

                var transformBlock = new TwString(rawBlock);
                await TransformScopeFunctionBlock(transformBlock, onlyProcessFirstChanceFunctions);
                content = content.Replace(rawBlock, transformBlock.ToString());
            }

            pageContent.Clear();
            pageContent.Append(content);
        }

        /// <summary>
        /// Transform blocks or sections of code, these are thinks like panels and alerts.
        /// </summary>
        /// <param name="pageContent"></param>
        /// <param name="onlyProcessFirstChanceFunctions">Only process early functions (like code blocks)</param>
        private async Task TransformScopeFunctionBlock(TwString pageContent, bool onlyProcessFirstChanceFunctions)
        {
            // {{([\\S\\s]*)}}
            var orderedMatches = WikiUtility.OrderMatchesByLengthDescending(
                PrecompiledRegex.TransformBlock().Matches(pageContent.ToString()));

            foreach (var match in orderedMatches)
            {
                var parsedFunction = ParsedFunction.Create(match.Value);

                try
                {
                    if (onlyProcessFirstChanceFunctions
                        && Engine.ScopeFunctions.TryGetFunctionDescriptor(parsedFunction, out var function)
                        && function.FunctionAttribute.IsFirstChance == false)
                    {
                        //We are only processing "first chance" functions, so skip processing this function.
                        continue;
                    }

                    var preparedFunction = PreparedFunction.Create(this, Engine.ScopeFunctions, parsedFunction);
                    var result = await preparedFunction.Execute();
                    StoreHandlerResult(result, TwMatchType.StandardFunction, pageContent, match.Value);
                }
                catch (Exception ex)
                {
                    await StoreWikiError(pageContent, match.Value, ex.Message);
                    continue;
                }
            }
        }

        /// <summary>
        /// Transform headings. These are the basic HTML H1-H6 headings but they are saved for the building of the table of contents.
        /// </summary>
        private async Task TransformHeadings(TwString pageContent)
        {
            var orderedMatches = WikiUtility.OrderMatchesByLengthDescending(
                PrecompiledRegex.TransformSectionHeadings().Matches(pageContent.ToString()));

            foreach (var match in orderedMatches)
            {
                int headingMarkers = 0;
                foreach (char c in match.Value)
                {
                    if (c != '=')
                    {
                        break;
                    }
                    headingMarkers++;
                }
                if (headingMarkers >= 2)
                {
                    string link = _tocName + "_" + TableOfContents.Count.ToString();
                    string text = match.Value.Substring(headingMarkers).Trim().Trim(['=']).Trim();

                    bool wasHandled = false;
                    foreach (var handler in Engine.HeadingHandlers)
                    {
                        var result = await handler.Handle(this, headingMarkers - 1, link, text);
                        if (!result.Instructions.Contains(TwResultInstruction.Skip))
                        {
                            wasHandled = true;
                            TableOfContents.Add(new TwTableOfContentsTag(headingMarkers - 1, match.Index, link, text));
                            StoreHandlerResult(result, TwMatchType.Heading, pageContent, match.Value);
                            break;
                        }
                    }
                    if (!wasHandled)
                    {
                        await StoreWikiError(pageContent, match.Value, $"No heading tag handler processed the instruction: \"{match.Value}\".");
                    }
                }
            }
        }

        private async Task TransformComments(TwString pageContent)
        {
            var orderedMatches = WikiUtility.OrderMatchesByLengthDescending(
                PrecompiledRegex.TransformComments().Matches(pageContent.ToString()));

            foreach (var match in orderedMatches)
            {
                bool wasHandled = false;
                foreach (var handler in Engine.CommentHandlers)
                {
                    var result = await handler.Handle(this, match.Value);
                    if (!result.Instructions.Contains(TwResultInstruction.Skip))
                    {
                        wasHandled = true;
                        StoreHandlerResult(result, TwMatchType.Comment, pageContent, match.Value);
                        break;
                    }
                }
                if (!wasHandled)
                {
                    await StoreWikiError(pageContent, match.Value, $"No commet tag handler processed the instruction: \"{match.Value}\".");
                }
            }
        }

        private async Task TransformEmoji(TwString pageContent)
        {
            var orderedMatches = WikiUtility.OrderMatchesByLengthDescending(
                PrecompiledRegex.TransformEmoji().Matches(pageContent.ToString()));

            foreach (var match in orderedMatches)
            {
                string key = match.Value.Trim().ToLowerInvariant().Trim('%');
                int scale = 100;

                var parts = key.Split(',');
                if (parts.Length > 1)
                {
                    key = parts[0]; //Image key;
                    scale = int.Parse(parts[1]); //Image scale.
                }

                bool wasHandled = false;
                foreach (var handler in Engine.EmojiHandlers)
                {
                    var result = await handler.Handle(this, $"%%{key}%%", scale);
                    if (!result.Instructions.Contains(TwResultInstruction.Skip))
                    {
                        wasHandled = true;
                        StoreHandlerResult(result, TwMatchType.Emoji, pageContent, match.Value);
                        break;
                    }
                }
                if (!wasHandled)
                {
                    await StoreWikiError(pageContent, match.Value, $"No emoji tag handler processed the instruction: \"{match.Value}\".");
                }
            }
        }

        /// <summary>
        /// Transform variables.
        /// </summary>
        private async Task TransformVariables(TwString pageContent)
        {
            var orderedMatches = WikiUtility.OrderMatchesByLengthDescending(
                PrecompiledRegex.TransformVariables().Matches(pageContent.ToString()));

            foreach (var match in orderedMatches)
            {
                string key = match.Value.Trim(['{', '}', ' ', '\t', '$']);
                if (key.Contains('='))
                {
                    var sections = key.Split('=');
                    key = sections[0].Trim();
                    var value = sections[1].Trim();

                    if (!Variables.TryAdd(key, value))
                    {
                        Variables[key] = value;
                    }

                    var identifier = StoreMatch(TwMatchType.Variable, pageContent, match.Value, "");
                    pageContent.Replace($"{identifier}\n", $"{identifier}"); //Kill trailing newline.
                }
                else
                {
                    if (Variables.TryGetValue(key, out string? value))
                    {
                        var identifier = StoreMatch(TwMatchType.Variable, pageContent, match.Value, value);
                        pageContent.Replace($"{identifier}\n", $"{identifier}"); //Kill trailing newline.

                    }
                    else
                    {
                        throw new Exception($"The wiki variable {key} is not defined. It should be set with ##Set() before calling Get().");
                    }
                }
            }
        }

        /// <summary>
        /// Transform links, these can be internal Wiki links or external links.
        /// </summary>
        private async Task TransformLinks(TwString pageContent)
        {
            //Parse external explicit links. eg. [[http://test.net]].
            var orderedMatches = WikiUtility.OrderMatchesByLengthDescending(
                PrecompiledRegex.TransformExplicitHTTPLinks().Matches(pageContent.ToString()));

            foreach (var match in orderedMatches)
            {
                string link = match.Value.Substring(2, match.Value.Length - 4).Trim();
                var args = ParsedFunction.ParseArgumentsAddParenthesis(link);

                string? text = null;
                string? image = null;

                if (args.Count > 1)
                {
                    text = args[1];
                    link = args[0];
                    string imageTag = "image:";

                    if (text.StartsWith(imageTag, StringComparison.InvariantCultureIgnoreCase))
                    {
                        image = text.Substring(imageTag.Length).Trim();
                        text = null;
                    }
                }
                else
                {
                    text = link;
                }

                bool wasHandled = false;
                foreach (var handler in Engine.ExternalLinkHandlers)
                {
                    var result = await handler.Handle(this, link, text, image);
                    if (!result.Instructions.Contains(TwResultInstruction.Skip))
                    {
                        wasHandled = true;
                        StoreHandlerResult(result, TwMatchType.Link, pageContent, match.Value);
                        break;
                    }
                }
                if (!wasHandled)
                {
                    await StoreWikiError(pageContent, match.Value, $"No external link tag handler processed the instruction: \"{match.Value}\".");
                }
            }

            //Parse external explicit links. eg. [[https://test.net]].
            orderedMatches = WikiUtility.OrderMatchesByLengthDescending(
                PrecompiledRegex.TransformExplicitHTTPsLinks().Matches(pageContent.ToString()));

            foreach (var match in orderedMatches)
            {
                string link = match.Value.Substring(2, match.Value.Length - 4).Trim();
                var args = ParsedFunction.ParseArgumentsAddParenthesis(link);

                string? text = null;
                string? image = null;

                if (args.Count > 1)
                {
                    link = args[0];
                    text = args[1];

                    string imageTag = "image:";
                    image = null;

                    if (text.StartsWith(imageTag, StringComparison.InvariantCultureIgnoreCase))
                    {
                        image = text.Substring(imageTag.Length).Trim();
                        text = null;
                    }
                }
                else
                {
                    text = link;
                }

                bool wasHandled = false;
                foreach (var handler in Engine.ExternalLinkHandlers)
                {
                    var result = await handler.Handle(this, link, text, image);
                    if (!result.Instructions.Contains(TwResultInstruction.Skip))
                    {
                        wasHandled = true;
                        StoreHandlerResult(result, TwMatchType.Link, pageContent, match.Value);
                        break;
                    }
                }
                if (!wasHandled)
                {
                    await StoreWikiError(pageContent, match.Value, $"No external link tag handler processed the instruction: \"{match.Value}\".");
                }
            }

            //Parse internal links. eg [[About Us]], [[About Us, Learn about us]], etc..
            orderedMatches = WikiUtility.OrderMatchesByLengthDescending(
                PrecompiledRegex.TransformInternalDynamicLinks().Matches(pageContent.ToString()));

            foreach (var match in orderedMatches)
            {
                string keyword = match.Value.Substring(2, match.Value.Length - 4);

                var args = ParsedFunction.ParseArgumentsAddParenthesis(keyword);

                string pageName;
                string text;
                string? image = null;
                int imageScale = 100;

                if (args.Count == 1)
                {
                    //Page navigation only.
                    text = WikiUtility.GetPageNamePart(args[0]); //Text will be page name since we have an image.
                    pageName = args[0];
                }
                else if (args.Count >= 2)
                {
                    //Page navigation and explicit text (possibly image).
                    pageName = args[0];

                    string imageTag = "image:";
                    if (args[1].StartsWith(imageTag, StringComparison.InvariantCultureIgnoreCase))
                    {
                        image = args[1].Substring(imageTag.Length).Trim();
                        text = WikiUtility.GetPageNamePart(args[0]); //Text will be page name since we have an image.
                    }
                    else
                    {
                        text = args[1]; //Explicit text.
                    }

                    if (args.Count >= 3)
                    {
                        //Get the specified image scale.
                        if (int.TryParse(args[2], out imageScale) == false)
                        {
                            imageScale = 100;
                        }
                    }
                }
                else
                {
                    await StoreWikiError(pageContent, match.Value, $"The external link contains no page name: \"{match.Value}\".");
                    continue;
                }

                var pageNavigation = new TwNamespaceNavigation(pageName);

                if (pageName.Trim().StartsWith("::"))
                {
                    //The user explicitly specified the root (unnamed) namespace. 
                }
                else if (string.IsNullOrEmpty(pageNavigation.Namespace))
                {
                    //No namespace was specified, use the current page namespace.
                    pageNavigation.Namespace = Page.Namespace;
                }
                else
                {
                    //Use the namespace that the user explicitly specified.
                }

                bool wasHandled = false;
                foreach (var handler in Engine.InternalLinkHandlers)
                {
                    var result = await handler.Handle(this, pageNavigation, pageName.Trim(':'), text, image, imageScale);
                    if (!result.Instructions.Contains(TwResultInstruction.Skip))
                    {
                        wasHandled = true;
                        OutgoingLinks.Add(new TwPageReference(pageName, pageNavigation.Canonical));
                        StoreHandlerResult(result, TwMatchType.Link, pageContent, match.Value);
                        break;
                    }
                }
                if (!wasHandled)
                {
                    await StoreWikiError(pageContent, match.Value, $"No internal link tag handler processed the instruction: \"{match.Value}\".");
                }
            }
        }

        /// <summary>
        /// Transform processing instructions are used to flag pages for specific needs such as deletion, review, draft, etc.
        /// </summary>
        private async Task TransformProcessingInstructionFunctions(TwString pageContent)
        {
            // <code>(\\@\\@[\\w-]+\\(\\))|(\\@\\@[\\w-]+\\(.*?\\))|(\\@\\@[\\w-]+)</code><br/>
            var orderedMatches = WikiUtility.OrderMatchesByLengthDescending(
                PrecompiledRegex.TransformProcessingInstructions().Matches(pageContent.ToString()));

            var processingFunctions = Engine.ProcessingFunctions
                .Where(o => o.FunctionAttribute.IsPostProcess == false).ToList();

            foreach (var match in orderedMatches)
            {
                var parsedFunction = ParsedFunction.Create(match.Value);

                try
                {
                    var preparedFunction = PreparedFunction.Create(this, processingFunctions, parsedFunction);
                    var result = await preparedFunction.Execute();
                    StoreHandlerResult(result, TwMatchType.StandardFunction, pageContent, match.Value);
                }
                catch (Exception ex)
                {
                    await StoreWikiError(pageContent, match.Value, ex.Message);
                    continue;
                }
            }
        }

        /// <summary>
        /// Transform functions is used to call wiki functions such as including template pages, setting tags and displaying images.
        /// </summary>
        private async Task TransformStandardFunctions(TwString pageContent, bool onlyProcessFirstChanceFunctions)
        {
            //Remove the last "(\#\#[\w-]+)" if you start to have matching problems:
            var orderedMatches = WikiUtility.OrderMatchesByLengthDescending(
                PrecompiledRegex.TransformFunctions().Matches(pageContent.ToString()));

            foreach (var match in orderedMatches)
            {
                var parsedFunction = ParsedFunction.Create(match.Value);

                try
                {
                    var preparedFunction = PreparedFunction.Create(this, Engine.StandardFunctions, parsedFunction);

                    if (!Engine.StandardFunctions.TryGetFunctionDescriptor(parsedFunction, out var function))
                    {
                        throw new Exception($"The function {parsedFunction.Name} is not recognized.");
                    }

                    if (onlyProcessFirstChanceFunctions && function.FunctionAttribute.IsFirstChance == false)
                    {
                        //We are only processing "first chance" functions, so skip processing this function.
                        continue;
                    }

                    if (function.FunctionAttribute.IsPostProcess)
                    {
                        //This IS a function, but it is meant to be parsed at the end of processing.
                        continue;
                    }

                    var result = await preparedFunction.Execute();
                    StoreHandlerResult(result, TwMatchType.StandardFunction, pageContent, match.Value);
                }
                catch (Exception ex)
                {
                    await StoreWikiError(pageContent, match.Value, ex.Message);
                    continue;
                }
            }
        }

        /// <summary>
        /// Transform post-process are functions that must be called after all other transformations.
        /// For example, we can't build a table-of-contents until we have parsed the entire page.
        /// </summary>
        private async Task TransformPostProcessingFunctions(TwString pageContent)
        {
            //Remove the last "(\#\#[\w-]+)" if you start to have matching problems:
            var orderedMatches = WikiUtility.OrderMatchesByLengthDescending(
                PrecompiledRegex.TransformPostProcess().Matches(pageContent.ToString()));

            var postProcessingFunctions = Engine.StandardFunctions
                .Where(o => o.FunctionAttribute.IsPostProcess == true).ToList();

            foreach (var match in orderedMatches)
            {
                var parsedFunction = ParsedFunction.Create(match.Value);

                try
                {
                    var preparedFunction = PreparedFunction.Create(this, postProcessingFunctions, parsedFunction);
                    var result = await preparedFunction.Execute();
                    StoreHandlerResult(result, TwMatchType.StandardFunction, pageContent, match.Value);
                }
                catch (Exception ex)
                {
                    await StoreWikiError(pageContent, match.Value, ex.Message);
                    continue;
                }
            }
        }

        private static void TransformNewlines(TwString pageContent)
        {
            var identifier = NewIdentiferTag();

            //Replace new-lines with single character new line:
            pageContent.Replace("\r\n", "\n");

            //Replace new-lines with an identifier so we can identify the places we are going to introduce line-breaks:
            pageContent.Replace("\n", identifier);

            //Replace any consecutive to-be-line-breaks that we are introducing with single line-break identifiers.
            pageContent.Replace($"{identifier}{identifier}", identifier);

            //Swap in the real line-breaks.
            pageContent.Replace(identifier, "<br />");
        }

        #region Utility.

        private void StoreHandlerResult(TwPluginResult result, TwMatchType matchType, TwString pageContent, string matchValue)
        {
            if (result.Instructions.Contains(TwResultInstruction.Skip))
            {
                return;
            }

            bool allowNestedDecode = !result.Instructions.Contains(TwResultInstruction.DisallowNestedProcessing);

            string identifier;

            if (result.Instructions.Contains(TwResultInstruction.OnlyReplaceFirstMatch))
            {
                identifier = StoreFirstMatch(matchType, pageContent, matchValue, result.Content, allowNestedDecode);
            }
            else
            {
                identifier = StoreMatch(matchType, pageContent, matchValue, result.Content, allowNestedDecode);
            }

            foreach (var instruction in result.Instructions)
            {
                switch (instruction)
                {
                    case TwResultInstruction.TruncateTrailingLine:
                        pageContent.Replace($"{identifier}\n", $"{identifier}"); //Kill trailing newline.
                        break;
                }
            }
        }

        /// <summary>
        /// Used to store errors that halt the wiki processing and display a warning card with the error message on the page.
        /// This is still just a wiki error, not a serious server error.
        /// </summary>
        /// <param name="ex"></param>
        private async Task StoreCriticalWikiError(Exception ex)
        {
            foreach (var handler in Engine.ExceptionHandlers)
            {
                await handler.Handle(this, LogLevel.Warning, $"Page: [{Page.Navigation}], Revision: [{Page.Revision}]", ex);
            }

            ErrorCount++;
            HtmlResult = WikiUtility.WarningCard("Wiki Parser Exception", ex.Message);
        }

        private async Task<string> StoreWikiError(TwString pageContent, string match, string value)
        {
            foreach (var handler in Engine.ExceptionHandlers)
            {
                await handler.Handle(this, LogLevel.Debug, $"Page: [{Page.Navigation}], Revision: [{Page.Revision}], Error: [{value}]");
            }

            ErrorCount++;
            _matchesStoredPerIteration++;

            var identifier = NewIdentiferTag();

            var matchSet = new MatchSet()
            {
                Content = $"<i><font size=\"3\" color=\"#BB0000\">{{{value}}}</font></a>",
                AllowNestedDecode = false,
                MatchType = TwMatchType.Error
            };

            Matches.Add(identifier, matchSet);
            pageContent.Replace(match, identifier);

            return identifier;
        }

        private string StoreMatch(TwMatchType matchType, TwString pageContent, string match, string value, bool allowNestedDecode = true)
        {
            MatchCount++;
            _matchesStoredPerIteration++;

            var identifier = NewIdentiferTag();

            var matchSet = new MatchSet()
            {
                MatchType = matchType,
                Content = value,
                AllowNestedDecode = allowNestedDecode
            };

            Matches.Add(identifier, matchSet);
            pageContent.Replace(match, identifier);

            return identifier;
        }

        private string StoreFirstMatch(TwMatchType matchType, TwString pageContent, string match, string value, bool allowNestedDecode = true)
        {
            MatchCount++;
            _matchesStoredPerIteration++;

            var identifier = NewIdentiferTag();

            var matchSet = new MatchSet()
            {
                MatchType = matchType,
                Content = value,
                AllowNestedDecode = allowNestedDecode
            };
            Matches.Add(identifier, matchSet);

            var pageContentCopy = NTDLS.Helpers.Text.ReplaceFirstOccurrence(pageContent.ToString(), match, identifier);
            pageContent.Clear();
            pageContent.Append(pageContentCopy);

            return identifier;
        }

        /// <summary>
        /// Used to generate unique and regenerable tokens so different wikification process can identify
        ///     their own query strings. For instance, we can have more than one pager on a wiki page, this
        /// allows each pager to track its own current page in the query string.
        /// </summary>
        public string GetNextHttpQueryToken()
        {
            _queryTokenHash = SecurityUtility.Sha256(SecurityUtility.EncryptString(SecurityUtility.MachineKey, _queryTokenHash));
            return $"H{SecurityUtility.Crc32(_queryTokenHash)}";
        }

        #endregion
    }
}
