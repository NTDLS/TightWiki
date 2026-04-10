using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using TightWiki.Engine.Module.Function;
using TightWiki.Library;
using TightWiki.Library.Security;
using TightWiki.Plugin;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Function;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Models;

namespace TightWiki.Engine
{
    public class WikiEngineState
        : ITwEngineState
    {
        public bool IsLite { get; private set; } = false;
        public ITwEngine Engine { get; private set; }
        public ITwSharedLocalizationText Localizer { get; private set; }

        private string _queryTokenHash = "c03a1c9e-da83-479b-87e8-21d7906bd866";
        private int _matchesStoredPerIteration = 0;

        /// <summary>
        /// The HTML tag name used to identify anything that needs a name. Use in concuntion with the GetNextStepNumber()
        /// method to generate unique identifiers for tags during processing.
        /// </summary>
        public string TagMarker { get; private set; }
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

        private int _stepNumber = 0;
        /// <summary>
        /// Gets the next string to use for generating unique tag identifiers during processing, incrementing the internal counter to ensure uniqueness.
        /// </summary>
        /// <param name="prefix">String to be prepended to the result</param>
        public string GetNextTagMarker(string prefix)
        {
            return $"{prefix}_{TagMarker}_{_stepNumber++}";
        }

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
            ITwPage page, int? revision = null, TwMatchType[]? omitMatches = null, int nestDepth = 0, bool isLite = false)
        {
            IsLite = isLite;
            Localizer = localizer;
            Logger = logger;
            QueryString = session?.QueryString ?? new EmptyQueryCollection();
            Page = page;
            Revision = revision;
            Matches = new Dictionary<string, MatchSet>();
            Session = session;
            NestDepth = nestDepth;

            TagMarker = $"Tw{Math.Abs(SecurityUtility.Crc32(SecurityUtility.Sha1(page.Name))):X8}";

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
            await TransformMarkup(pageContent, true);
            await TransformScopeFunctions(pageContent, true); //First pass for "first chance" functions.
            await TransformStandardFunctions(pageContent, true); //First pass for "first chance" functions.

            await TransformMarkup(pageContent, false);
            await TransformScopeFunctions(pageContent, false); //Second pass for all functions.
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
            var orderedMatches = OrderMatchesByLengthDescending(
                PrecompiledRegex.ScopeFunctionBlock().Matches(pageContent.ToString()));

            var scopeFunctions = Engine.ScopeFunctions
                .Where(o => IsLite == false || o.FunctionAttribute.IsLitePermissiable == true).ToList();

            foreach (var match in orderedMatches)
            {
                var parsedFunction = ParsedFunction.Parse(match.Value);

                try
                {
                    if (onlyProcessFirstChanceFunctions
                        && scopeFunctions.TryGetFunctionDescriptor(parsedFunction, out var function)
                        && function.FunctionAttribute.IsFirstChance == false)
                    {
                        //We are only processing "first chance" functions, so skip processing this function.
                        continue;
                    }

                    var preparedFunction = PreparedFunction.Create(this, scopeFunctions, parsedFunction);
                    var result = await preparedFunction.Execute();
                    StoreHandlerResult(result, TwMatchType.ScopeFunction, pageContent, match.Value);
                }
                catch (Exception ex)
                {
                    await StoreWikiError(pageContent, match.Value, ex.Message);
                    continue;
                }
            }
        }

        private async Task TransformMarkup(TwString pageContent, bool onlyProcessFirstChanceHandlers)
        {
            var handlers = Engine.MarkupHandlers
                .Where(o => IsLite == false || o.HandlerAttribute.IsLitePermissiable == true)
                .OrderBy(h => h.PluginAttribute.Precedence)
                .ThenBy(h => h.HandlerAttribute.Precedence)
                .ToList();

            foreach (var handler in handlers)
            {
                if (onlyProcessFirstChanceHandlers && handler.HandlerAttribute.IsFirstChance == false)
                {
                    //We are only processing "first chance" handlers, so skip processing this function.
                    continue;
                }

                foreach (var expression in handler.ExpressionAttributes)
                {
                    RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
                            | (expression.Multiline ? RegexOptions.Multiline : RegexOptions.None);

                    var regex = new Regex(expression.Pattern, options);

                    var orderedMatches = OrderMatchesByLengthDescending(regex.Matches(pageContent.ToString()));

                    foreach (var match in orderedMatches)
                    {
                        var result = await handler.Handle(this, match);
                        if (!result.Instructions.Contains(TwResultInstruction.Skip))
                        {
                            StoreHandlerResult(result, TwMatchType.Markup, pageContent, match.Value);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Transform processing instructions are used to flag pages for specific needs such as deletion, review, draft, etc.
        /// </summary>
        private async Task TransformProcessingInstructionFunctions(TwString pageContent)
        {
            // <code>(\\@\\@[\\w-]+\\(\\))|(\\@\\@[\\w-]+\\(.*?\\))|(\\@\\@[\\w-]+)</code><br/>
            var orderedMatches = OrderMatchesByLengthDescending(
                PrecompiledRegex.ProcessingInstructionBlock().Matches(pageContent.ToString()));

            var processingFunctions = Engine.ProcessingFunctions
                .Where(o => o.FunctionAttribute.IsPostProcess == false).ToList();

            foreach (var match in orderedMatches)
            {
                var parsedFunction = ParsedFunction.Parse(match.Value);

                try
                {
                    var preparedFunction = PreparedFunction.Create(this, processingFunctions, parsedFunction);
                    var result = await preparedFunction.Execute();
                    StoreHandlerResult(result, TwMatchType.ProcessingInstruction, pageContent, match.Value);
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
            var orderedMatches = OrderMatchesByLengthDescending(
                PrecompiledRegex.StandardFunctionBlock().Matches(pageContent.ToString()));

            var standardFunctions = Engine.StandardFunctions
                .Where(o => IsLite == false || o.FunctionAttribute.IsLitePermissiable == true).ToList();

            foreach (var match in orderedMatches)
            {
                var parsedFunction = ParsedFunction.Parse(match.Value);

                try
                {
                    var preparedFunction = PreparedFunction.Create(this, standardFunctions, parsedFunction);

                    if (onlyProcessFirstChanceFunctions && preparedFunction.Descriptor.FunctionAttribute.IsFirstChance == false)
                    {
                        //We are only processing "first chance" functions, so skip processing this function.
                        continue;
                    }

                    if (preparedFunction.Descriptor.FunctionAttribute.IsPostProcess)
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
            var orderedMatches = OrderMatchesByLengthDescending(
                PrecompiledRegex.PostProcessBlock().Matches(pageContent.ToString()));

            var postProcessingFunctions = Engine.StandardFunctions
                .Where(o => o.FunctionAttribute.IsPostProcess == true).ToList();

            foreach (var match in orderedMatches)
            {
                var parsedFunction = ParsedFunction.Parse(match.Value);

                try
                {
                    var preparedFunction = PreparedFunction.Create(this, postProcessingFunctions, parsedFunction);
                    var result = await preparedFunction.Execute();
                    StoreHandlerResult(result, TwMatchType.PostProcessingFunction, pageContent, match.Value);
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

        internal static string WarningCard(string header, string exceptionText)
        {
            var html = new StringBuilder();

            html.AppendLine("<div class=\"card bg-warning mb-3\">");
            html.AppendLine($"  <div class=\"card-header\"><strong>{header}</strong></div>");
            html.AppendLine("  <div class=\"card-body\">");
            html.AppendLine($"    <p class=\"card-text mb-0\">{exceptionText}</p>");
            html.AppendLine("  </div>");
            html.AppendLine("</div>");

            return html.ToString();
        }

        internal static List<TwOrderedMatch> OrderMatchesByLengthDescending(MatchCollection matches)
        {
            var result = new List<TwOrderedMatch>();

            foreach (Match match in matches)
            {
                result.Add(new TwOrderedMatch
                {
                    Value = match.Value,
                    Index = match.Index
                });
            }

            return result.OrderByDescending(o => o.Value.Length).ToList();
        }

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
            HtmlResult = WarningCard("Wiki Parser Exception", ex.Message);
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
    }
}
