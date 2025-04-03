using Microsoft.AspNetCore.Http;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using TightWiki.Engine.Function;
using TightWiki.Engine.Function.Exceptions;
using TightWiki.Engine.Library;
using TightWiki.Engine.Library.Interfaces;
using TightWiki.Library;
using TightWiki.Library.Interfaces;
using static TightWiki.Engine.Library.Constants;

namespace TightWiki.Engine
{
    public class TightEngineState : ITightEngineState
    {
        public ITightEngine Engine { get; private set; }

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
        public List<PageReference> OutgoingLinks { get; private set; } = new();
        public List<string> ProcessingInstructions { get; private set; } = new();
        public string HtmlResult { get; private set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
        public Dictionary<string, WikiMatchSet> Matches { get; private set; } = new();
        public List<TableOfContentsTag> TableOfContents { get; } = new();
        public List<string> Headers { get; } = new();

        #endregion

        #region Input parameters.

        public IPage Page { get; }
        public int? Revision { get; }
        public IQueryCollection QueryString { get; }
        public ISessionState? Session { get; }
        public HashSet<WikiMatchType> OmitMatches { get; private set; } = new();
        public int NestDepth { get; private set; } //Used for recursion.

        #endregion

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
        internal TightEngineState(ITightEngine engine, ISessionState? session,
            IPage page, int? revision = null, WikiMatchType[]? omitMatches = null, int nestDepth = 0)
        {
            QueryString = session?.QueryString ?? new QueryCollection();
            Page = page;
            Revision = revision;
            Matches = new Dictionary<string, WikiMatchSet>();
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
        public ITightEngineState TransformChild(IPage page, int? revision = null)
        {
            return new TightEngineState(Engine, Session, page, revision, OmitMatches.ToArray(), NestDepth + 1).Transform();
        }

        internal ITightEngineState Transform()
        {
            var startTime = DateTime.UtcNow;

            try
            {
                var pageContent = new WikiString(Page.Body);

                pageContent.Replace("\r\n", "\n");

                TransformLiterals(pageContent);

                while (TransformAll(pageContent) > 0)
                {
                }

                TransformPostProcessingFunctions(pageContent);
                TransformWhitespace(pageContent);

                if (PageTitle != null)
                {
                    if (Matches.TryGetValue(PageTitle, out var pageTitle))
                    {
                        PageTitle = pageTitle.Content;
                    }
                }


                int length;
                do
                {
                    length = pageContent.Length;
                    foreach (var v in Matches)
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
                } while (length != pageContent.Length);

                pageContent.Replace(SoftBreak, "\r\n");
                pageContent.Replace(HardBreak, "<br />");

                //Prepend any headers that were added by wiki handlers.
                foreach (var header in Headers)
                {
                    pageContent.Insert(0, header);
                }

                HtmlResult = pageContent.ToString();
            }
            catch (Exception ex)
            {
                StoreCriticalError(ex);
            }

            ProcessingTime = DateTime.UtcNow - startTime;

            Engine.CompletionHandler.Complete(this);

            return this;
        }

        private int TransformAll(WikiString pageContent)
        {
            TransformIterations++;

            _matchesStoredPerIteration = 0;

            TransformComments(pageContent);
            TransformHeadings(pageContent);

            TransformScopeFunctions(pageContent);

            TransformVariables(pageContent);
            TransformLinks(pageContent);
            TransformMarkup(pageContent);
            TransformEmoji(pageContent);

            TransformStandardFunctions(pageContent, true);
            TransformStandardFunctions(pageContent, false);
            TransformProcessingInstructionFunctions(pageContent);

            //We have to replace a few times because we could have replace tags (guids) nested inside others.
            int length;
            do
            {
                length = pageContent.Length;
                foreach (var v in Matches)
                {
                    if (v.Value.AllowNestedDecode)
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

            return _matchesStoredPerIteration;
        }

        /// <summary>
        /// Transform basic markup such as bold, italics, underline, etc. for single and multi-line.
        /// </summary>
        private void TransformMarkup(WikiString pageContent)
        {
            var symbols = WikiUtility.GetApplicableSymbols(pageContent.Value);

            foreach (var symbol in symbols)
            {
                var sequence = new string(symbol, 2);
                var escapedSequence = Regex.Escape(sequence);

                var rgx = new Regex(@$"{escapedSequence}(.*?){escapedSequence}", RegexOptions.IgnoreCase);
                var orderedMatches = WikiUtility.OrderMatchesByLengthDescending(rgx.Matches(pageContent.ToString()));
                foreach (var match in orderedMatches)
                {
                    string body = match.Value.Substring(sequence.Length, match.Value.Length - sequence.Length * 2);

                    var result = Engine.MarkupHandler.Handle(this, symbol, body);

                    StoreHandlerResult(result, WikiMatchType.Markup, pageContent, match.Value);
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
                if (headingMarkers >= 2 && headingMarkers <= 6)
                {
                    string value = match.Value.Substring(headingMarkers).Trim();

                    int fontSize = 1 + headingMarkers;
                    if (fontSize < 1) fontSize = 1;

                    string markup = "<font size=\"" + fontSize + "\">" + value + "</font>\r\n";
                    StoreMatch(WikiMatchType.Markup, pageContent, match.Value, markup);
                }
            }
        }

        /// <summary>
        /// Transform inline and multi-line literal blocks. These are blocks where the content
        /// will not be wikified and contain code that is encoded to display verbatim on the page.
        /// </summary>
        private void TransformLiterals(WikiString pageContent)
        {
            //TODO: May need to do the same thing we did with TransformBlocks() to match all these if they need to be nested.

            //Transform literal strings, even encodes HTML so that it displays verbatim.
            var orderedMatches = WikiUtility.OrderMatchesByLengthDescending(
                PrecompiledRegex.TransformLiterals().Matches(pageContent.ToString()));

            foreach (var match in orderedMatches)
            {
                string value = match.Value.Substring(2, match.Value.Length - 4);
                value = HttpUtility.HtmlEncode(value);
                StoreMatch(WikiMatchType.Literal, pageContent, match.Value, value.Replace("\r", "").Trim().Replace("\n", "<br />\r\n"), false);
            }
        }

        /// <summary>
        /// Matching nested blocks with regex was hell, I escaped with a loop. ¯\_(ツ)_/¯
        /// </summary>
        private void TransformScopeFunctions(WikiString pageContent)
        {
            var content = pageContent.ToString();

            string rawBlock = string.Empty;

            int startPos = content.Length - 1;

            while (true)
            {
                startPos = content.LastIndexOf("{{", startPos);
                if (startPos < 0)
                {
                    break;
                }
                int endPos = content.IndexOf("}}", startPos);

                if (endPos < 0 || endPos < startPos)
                {
                    var exception = new StringBuilder();
                    exception.AppendLine($"<strong>A parsing error occurred after position {startPos}:<br /></strong> Unable to locate closing tag.<br /><br />");
                    if (rawBlock?.Length > 0)
                    {
                        exception.AppendLine($"<strong>The last successfully parsed block was:</strong><br /> {rawBlock}");
                    }
                    exception.AppendLine($"<strong>The problem occurred after:</strong><br /> {pageContent.ToString().Substring(startPos)}<br /><br />");
                    exception.AppendLine($"<strong>The content the parser was working on is:</strong><br /> {pageContent}<br /><br />");

                    throw new Exception(exception.ToString());
                }

                rawBlock = content.Substring(startPos, endPos - startPos + 2);
                var transformBlock = new WikiString(rawBlock);
                TransformScopeFunctions(transformBlock, true);
                TransformScopeFunctions(transformBlock, false);
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
        private void TransformScopeFunctions(WikiString pageContent, bool onlyProcessFirstChanceFunctions)
        {
            // {{([\\S\\s]*)}}
            var orderedMatches = WikiUtility.OrderMatchesByLengthDescending(
                PrecompiledRegex.TransformBlock().Matches(pageContent.ToString()));

            var functionHandler = Engine.ScopeFunctionHandler;

            foreach (var match in orderedMatches)
            {
                int paramEndIndex;

                FunctionCall function;

                var mockFunctionCall = "##" + match.Value.Trim([' ', '\t', '{', '}']);

                try
                {
                    function = FunctionParser.ParseAndGetFunctionCall(functionHandler.Prototypes, mockFunctionCall, out paramEndIndex);
                    if (onlyProcessFirstChanceFunctions && !!function.Prototype.IsFirstChance)
                    {
                        //We are only processing "first chance" functions, so skip processing this function.
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    StoreError(pageContent, match.Value, ex.Message);
                    continue;
                }

                string scopeBody = mockFunctionCall.Substring(paramEndIndex).Trim();

                try
                {
                    var result = functionHandler.Handle(this, function, scopeBody);
                    StoreHandlerResult(result, WikiMatchType.ScopeFunction, pageContent, match.Value);
                }
                catch (Exception ex)
                {
                    StoreError(pageContent, match.Value, ex.Message);
                }
            }
        }

        /// <summary>
        /// Transform headings. These are the basic HTML H1-H6 headings but they are saved for the building of the table of contents.
        /// </summary>
        /// <param name="pageContent"></param>
        private void TransformHeadings(WikiString pageContent)
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

                    var result = Engine.HeadingHandler.Handle(this, headingMarkers - 1, link, text);

                    if (!result.Instructions.Contains(HandlerResultInstruction.Skip))
                    {
                        TableOfContents.Add(new TableOfContentsTag(headingMarkers - 1, match.Index, link, text));
                    }

                    StoreHandlerResult(result, WikiMatchType.Heading, pageContent, match.Value);
                }
            }
        }

        private void TransformComments(WikiString pageContent)
        {
            var orderedMatches = WikiUtility.OrderMatchesByLengthDescending(
                PrecompiledRegex.TransformComments().Matches(pageContent.ToString()));

            foreach (var match in orderedMatches)
            {
                var result = Engine.CommentHandler.Handle(this, match.Value);
                StoreHandlerResult(result, WikiMatchType.Comment, pageContent, match.Value);
            }
        }

        private void TransformEmoji(WikiString pageContent)
        {
            var orderedMatches = WikiUtility.OrderMatchesByLengthDescending(
                PrecompiledRegex.TransformEmoji().Matches(pageContent.ToString()));

            foreach (var match in orderedMatches)
            {
                string key = match.Value.Trim().ToLower().Trim('%');
                int scale = 100;

                var parts = key.Split(',');
                if (parts.Length > 1)
                {
                    key = parts[0]; //Image key;
                    scale = int.Parse(parts[1]); //Image scale.
                }

                var result = Engine.EmojiHandler.Handle(this, $"%%{key}%%", scale);
                StoreHandlerResult(result, WikiMatchType.Emoji, pageContent, match.Value);
            }
        }

        /// <summary>
        /// Transform variables.
        /// </summary>
        /// <param name="pageContent"></param>
        private void TransformVariables(WikiString pageContent)
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

                    var identifier = StoreMatch(WikiMatchType.Variable, pageContent, match.Value, "");
                    pageContent.Replace($"{identifier}\n", $"{identifier}"); //Kill trailing newline.
                }
                else
                {
                    if (Variables.TryGetValue(key, out string? value))
                    {
                        var identifier = StoreMatch(WikiMatchType.Variable, pageContent, match.Value, value);
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
        /// <param name="pageContent"></param>
        private void TransformLinks(WikiString pageContent)
        {
            //Parse external explicit links. eg. [[http://test.net]].
            var orderedMatches = WikiUtility.OrderMatchesByLengthDescending(
                PrecompiledRegex.TransformExplicitHTTPLinks().Matches(pageContent.ToString()));

            foreach (var match in orderedMatches)
            {
                string link = match.Value.Substring(2, match.Value.Length - 4).Trim();
                var args = FunctionParser.ParseArgumentsAddParenthesis(link);

                if (args.Count > 1)
                {
                    link = args[0];
                    string? text = args[1];

                    string imageTag = "image:";
                    string? image = null;

                    if (text.StartsWith(imageTag, StringComparison.CurrentCultureIgnoreCase))
                    {
                        image = text.Substring(imageTag.Length).Trim();
                        text = null;
                    }

                    var result = Engine.ExternalLinkHandler.Handle(this, link, text, image);
                    StoreHandlerResult(result, WikiMatchType.Link, pageContent, match.Value);
                }
                else
                {
                    var result = Engine.ExternalLinkHandler.Handle(this, link, link, null);
                    StoreHandlerResult(result, WikiMatchType.Link, pageContent, match.Value);
                }
            }

            //Parse external explicit links. eg. [[https://test.net]].
            orderedMatches = WikiUtility.OrderMatchesByLengthDescending(
                PrecompiledRegex.TransformExplicitHTTPsLinks().Matches(pageContent.ToString()));

            foreach (var match in orderedMatches)
            {
                string link = match.Value.Substring(2, match.Value.Length - 4).Trim();
                var args = FunctionParser.ParseArgumentsAddParenthesis(link);

                if (args.Count > 1)
                {
                    link = args[0];
                    string? text = args[1];

                    string imageTag = "image:";
                    string? image = null;

                    if (text.StartsWith(imageTag, StringComparison.CurrentCultureIgnoreCase))
                    {
                        image = text.Substring(imageTag.Length).Trim();
                        text = null;
                    }

                    var result = Engine.ExternalLinkHandler.Handle(this, link, text, image);
                    StoreHandlerResult(result, WikiMatchType.Link, pageContent, match.Value);
                }
                else
                {
                    var result = Engine.ExternalLinkHandler.Handle(this, link, link, null);
                    StoreHandlerResult(result, WikiMatchType.Link, pageContent, match.Value);
                }
            }

            //Parse internal links. eg [[About Us]], [[About Us, Learn about us]], etc..
            orderedMatches = WikiUtility.OrderMatchesByLengthDescending(
                PrecompiledRegex.TransformInternalDynamicLinks().Matches(pageContent.ToString()));

            foreach (var match in orderedMatches)
            {
                string keyword = match.Value.Substring(2, match.Value.Length - 4);

                var args = FunctionParser.ParseArgumentsAddParenthesis(keyword);

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
                    if (args[1].StartsWith(imageTag, StringComparison.CurrentCultureIgnoreCase))
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
                    StoreError(pageContent, match.Value, "The external link contains no page name.");
                    continue;
                }

                var pageNavigation = new NamespaceNavigation(pageName);

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

                var result = Engine.InternalLinkHandler.Handle(this, pageNavigation, pageName.Trim(':'), text, image, imageScale);
                if (!result.Instructions.Contains(HandlerResultInstruction.Skip))
                {
                    OutgoingLinks.Add(new PageReference(pageName, pageNavigation.Canonical));
                }

                StoreHandlerResult(result, WikiMatchType.Link, pageContent, match.Value);
            }
        }

        /// <summary>
        /// Transform processing instructions are used to flag pages for specific needs such as deletion, review, draft, etc.
        /// </summary>
        /// <param name="pageContent"></param>
        private void TransformProcessingInstructionFunctions(WikiString pageContent)
        {
            // <code>(\\@\\@[\\w-]+\\(\\))|(\\@\\@[\\w-]+\\(.*?\\))|(\\@\\@[\\w-]+)</code><br/>
            var orderedMatches = WikiUtility.OrderMatchesByLengthDescending(
                PrecompiledRegex.TransformProcessingInstructions().Matches(pageContent.ToString()));

            var functionHandler = Engine.ProcessingInstructionFunctionHandler;

            foreach (var match in orderedMatches)
            {
                FunctionCall function;

                try
                {
                    function = FunctionParser.ParseAndGetFunctionCall(functionHandler.Prototypes, match.Value, out int matchEndIndex);
                }
                catch (Exception ex)
                {
                    StoreError(pageContent, match.Value, ex.Message);
                    continue;
                }

                try
                {
                    var result = functionHandler.Handle(this, function, string.Empty);
                    StoreHandlerResult(result, WikiMatchType.Instruction, pageContent, match.Value);
                }
                catch (Exception ex)
                {
                    StoreError(pageContent, match.Value, ex.Message);
                }
            }
        }

        /// <summary>
        /// Transform functions is used to call wiki functions such as including template pages, setting tags and displaying images.
        /// </summary>
        /// <param name="pageContent"></param>
        private void TransformStandardFunctions(WikiString pageContent, bool isFirstChance)
        {
            //Remove the last "(\#\#[\w-]+)" if you start to have matching problems:
            var orderedMatches = WikiUtility.OrderMatchesByLengthDescending(
                PrecompiledRegex.TransformFunctions().Matches(pageContent.ToString()));

            var functionHandler = Engine.StandardFunctionHandler;

            foreach (var match in orderedMatches)
            {
                FunctionCall function;

                try
                {
                    function = FunctionParser.ParseAndGetFunctionCall(functionHandler.Prototypes, match.Value, out int matchEndIndex);
                }
                catch (WikiFunctionPrototypeNotDefinedException ex)
                {
                    var postProcessPrototypes = Engine.PostProcessingFunctionHandler.Prototypes;
                    var parsed = FunctionParser.ParseFunctionCall(match.Value);

                    if (parsed != default && postProcessPrototypes.Exists(parsed.Demarcation, parsed.Name))
                    {
                        continue; //This IS a function, but it is meant to be parsed at the end of processing.
                    }
                    StoreError(pageContent, match.Value, ex.Message);
                    continue;
                }
                catch (Exception ex)
                {
                    StoreError(pageContent, match.Value, ex.Message);
                    continue;
                }

                var firstChanceFunctions = new string[] { "include", "inject" }; //Process these the first time through.
                if (isFirstChance && firstChanceFunctions.Contains(function.Name.ToLower()) == false)
                {
                    continue;
                }

                try
                {
                    var result = functionHandler.Handle(this, function, string.Empty);
                    StoreHandlerResult(result, WikiMatchType.StandardFunction, pageContent, match.Value);
                }
                catch (Exception ex)
                {
                    StoreError(pageContent, match.Value, ex.Message);
                }
            }
        }

        /// <summary>
        /// Transform post-process are functions that must be called after all other transformations.
        /// For example, we can't build a table-of-contents until we have parsed the entire page.
        /// </summary>
        private void TransformPostProcessingFunctions(WikiString pageContent)
        {
            //Remove the last "(\#\#[\w-]+)" if you start to have matching problems:
            var orderedMatches = WikiUtility.OrderMatchesByLengthDescending(
                PrecompiledRegex.TransformPostProcess().Matches(pageContent.ToString()));

            var functionHandler = Engine.PostProcessingFunctionHandler;

            foreach (var match in orderedMatches)
            {
                FunctionCall function;

                try
                {
                    function = FunctionParser.ParseAndGetFunctionCall(functionHandler.Prototypes, match.Value, out int matchEndIndex);
                }
                catch (Exception ex)
                {
                    StoreError(pageContent, match.Value, ex.Message);
                    continue;
                }

                try
                {
                    var result = functionHandler.Handle(this, function, string.Empty);
                    StoreHandlerResult(result, WikiMatchType.StandardFunction, pageContent, match.Value);
                }
                catch (Exception ex)
                {
                    StoreError(pageContent, match.Value, ex.Message);
                }
            }
        }

        private static void TransformWhitespace(WikiString pageContent)
        {
            string identifier = $"<!--{Guid.NewGuid()}-->";

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

        private void StoreHandlerResult(HandlerResult result, WikiMatchType matchType, WikiString pageContent, string matchValue)
        {
            if (result.Instructions.Contains(HandlerResultInstruction.Skip))
            {
                return;
            }

            bool allowNestedDecode = !result.Instructions.Contains(HandlerResultInstruction.DisallowNestedProcessing);

            string identifier;

            if (result.Instructions.Contains(HandlerResultInstruction.OnlyReplaceFirstMatch))
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
                    case HandlerResultInstruction.TruncateTrailingLine:
                        pageContent.Replace($"{identifier}\n", $"{identifier}"); //Kill trailing newline.
                        break;
                }
            }
        }

        private void StoreCriticalError(Exception ex)
        {
            Engine.ExceptionHandler.Log(this, ex, $"Page: {Page.Navigation}, Error: {ex.Message}");

            ErrorCount++;
            HtmlResult = WikiUtility.WarningCard("Wiki Parser Exception", ex.Message);
        }

        private string StoreError(WikiString pageContent, string match, string value)
        {
            Engine.ExceptionHandler.Log(this, null, $"Page: {Page.Navigation}, Error: {value}");

            ErrorCount++;
            _matchesStoredPerIteration++;

            string identifier = $"<!--{Guid.NewGuid()}-->";

            var matchSet = new WikiMatchSet()
            {
                Content = $"<i><font size=\"3\" color=\"#BB0000\">{{{value}}}</font></a>",
                AllowNestedDecode = false,
                MatchType = WikiMatchType.Error
            };

            Matches.Add(identifier, matchSet);
            pageContent.Replace(match, identifier);

            return identifier;
        }

        private string StoreMatch(WikiMatchType matchType, WikiString pageContent, string match, string value, bool allowNestedDecode = true)
        {
            MatchCount++;
            _matchesStoredPerIteration++;

            string identifier = $"<!--{Guid.NewGuid()}-->";

            var matchSet = new WikiMatchSet()
            {
                MatchType = matchType,
                Content = value,
                AllowNestedDecode = allowNestedDecode
            };

            Matches.Add(identifier, matchSet);
            pageContent.Replace(match, identifier);

            return identifier;
        }

        private string StoreFirstMatch(WikiMatchType matchType, WikiString pageContent, string match, string value, bool allowNestedDecode = true)
        {
            MatchCount++;
            _matchesStoredPerIteration++;

            string identifier = $"<!--{Guid.NewGuid()}-->";

            var matchSet = new WikiMatchSet()
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
        public string GetNextQueryToken()
        {
            _queryTokenHash = Security.Helpers.Sha256(Security.Helpers.EncryptString(Security.Helpers.MachineKey, _queryTokenHash));
            return $"H{Security.Helpers.Crc32(_queryTokenHash)}";
        }

        #endregion
    }
}
