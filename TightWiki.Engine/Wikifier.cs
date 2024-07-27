using DuoVia.FuzzyStrings;
using Microsoft.AspNetCore.Http;
using NTDLS.Helpers;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using TightWiki.Configuration;
using TightWiki.Engine.Library;
using TightWiki.Engine.Library.Interfaces;
using TightWiki.EngineFunction;
using TightWiki.Library.Interfaces;
using TightWiki.Repository;
using static TightWiki.Engine.Library.Constants;

namespace TightWiki.Engine
{
    public partial class Wikifier : IWikifier
    {
        private readonly IFunctionHandler _scopeFunctionHandler;
        private readonly IFunctionHandler _standardFunctionHandler;
        private readonly IFunctionHandler _processingInstructionFunctionHandler;
        private readonly IFunctionHandler _standardPostProcessingFunctionHandler;
        private string _queryTokenState = Security.Helpers.MachineKey;
        private int _matchesPerIteration = 0;
        private readonly string _tocName = "TOC_" + new Random().Next(0, 1000000).ToString();
        private readonly HashSet<WikiMatchType> _omitMatches = new();

        #region Public properties.

        public int ErrorCount { get; private set; }
        public int MatchCount { get; private set; }
        public TimeSpan ProcessingTime { get; private set; }
        public Dictionary<string, string> Variables { get; } = new();
        public Dictionary<string, string> Snippets { get; } = new();
        public List<NameNav> OutgoingLinks { get; private set; } = new();
        public List<string> ProcessingInstructions { get; private set; } = new();
        public string ProcessedBody { get; private set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
        public Dictionary<string, MatchSet> Matches { get; private set; } = new();
        public IPage Page { get; }
        public int? Revision { get; }
        public IQueryCollection QueryString { get; }
        public ISessionState? SessionState { get; }
        public List<TOCTag> TableOfContents { get; } = new();
        public List<string> Headers { get; } = new();
        public int CurrentNestLevel { get; }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="standardFunctionHandler">Handler for standard functions</param>
        /// <param name="scopeFunctionHandler">Handler for scope functions.</param>
        /// <param name="processingInstructionHandler">Handler for processing instructions.</param>
        /// <param name="standardFunctionPostProcessHandler">Handler for post process functions.</param>
        /// <param name="sessionState">The users current state, used for localization.</param>
        /// <param name="page">The page that is being processed.</param>
        /// <param name="revision">The revision of the page that is being processed.</param>
        /// <param name="omitMatches">The type of matches that we want to omit from processing.</param>
        /// <param name="nestLevel">Internal use only, used for recursive processing.</param>
        public Wikifier(IFunctionHandler standardFunctionHandler,
            IFunctionHandler scopeFunctionHandler,
            IFunctionHandler processingInstructionHandler,
            IFunctionHandler standardFunctionPostProcessHandler,
            ISessionState? sessionState, IPage page, int? revision = null,
             WikiMatchType[]? omitMatches = null, int nestLevel = 0)
        {
            DateTime startTime = DateTime.UtcNow;

            _scopeFunctionHandler = scopeFunctionHandler;
            _standardFunctionHandler = standardFunctionHandler;
            _processingInstructionFunctionHandler = processingInstructionHandler;
            _standardPostProcessingFunctionHandler = standardFunctionPostProcessHandler;

            CurrentNestLevel = nestLevel;
            QueryString = sessionState?.QueryString ?? new QueryCollection();
            Page = page;
            Revision = revision;
            Matches = new Dictionary<string, MatchSet>();
            SessionState = sessionState;

            if (omitMatches != null)
            {
                _omitMatches.UnionWith(omitMatches);
            }

            try
            {
                Transform();
            }
            catch (Exception ex)
            {
                StoreCriticalError(ex);
            }

            ProcessingTime = DateTime.UtcNow - startTime;

            if (GlobalConfiguration.RecordCompilationMetrics)
            {
                StatisticsRepository.InsertCompilationStatistics(page.Id,
                    ProcessingTime.TotalMilliseconds,
                    MatchCount,
                    ErrorCount,
                    OutgoingLinks.Count,
                    Tags.Count,
                    ProcessedBody.Length,
                    page.Body.Length);
            }
        }

        public IWikifier CreateChildWikifier(IPage page)
        {
            return new Wikifier(
                _standardFunctionHandler,
                _scopeFunctionHandler,
                _processingInstructionFunctionHandler,
                _standardPostProcessingFunctionHandler,
                SessionState, page, null, _omitMatches.ToArray(), CurrentNestLevel + 1);
        }

        public List<WeightedToken> ParsePageTokens()
        {
            var allTokens = new List<WeightedToken>();

            allTokens.AddRange(WikiUtility.ParsePageTokens(ProcessedBody, 1));
            allTokens.AddRange(WikiUtility.ParsePageTokens(Page.Description, 1.2));
            allTokens.AddRange(WikiUtility.ParsePageTokens(string.Join(" ", Tags), 1.4));
            allTokens.AddRange(WikiUtility.ParsePageTokens(Page.Name, 1.6));

            allTokens = allTokens.GroupBy(o => o.Token).Select(o => new WeightedToken
            {
                Token = o.Key,
                DoubleMetaphone = o.Key.ToDoubleMetaphone(),
                Weight = o.Sum(g => g.Weight)
            }).ToList();

            return allTokens;
        }

        private void Transform()
        {
            var pageContent = new WikiString(Page.Body);

            pageContent.Replace("\r\n", "\n");

            TransformLiterals(pageContent);

            while (TransformAll(pageContent) > 0)
            {
            }

            TransformPostProcess(pageContent);
            TransformWhitespace(pageContent);

            int length;
            do
            {
                length = pageContent.Length;
                foreach (var v in Matches)
                {
                    if (_omitMatches.Contains(v.Value.MatchType))
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

            ProcessedBody = pageContent.ToString();
        }

        public int TransformAll(WikiString pageContent)
        {
            _matchesPerIteration = 0;

            TransformComments(pageContent);
            TransformSectionHeadings(pageContent);

            TransformScopeFunctions(pageContent);

            TransformVariables(pageContent);
            TransformLinks(pageContent);
            TransformMarkup(pageContent);
            TransformEmoji(pageContent);

            TransformStandardFunctions(pageContent, true);
            TransformStandardFunctions(pageContent, false);
            TransformProcessingInstructions(pageContent);

            //We have to replace a few times because we could have replace tags (guids) nested inside others.
            int length;
            do
            {
                length = pageContent.Length;
                foreach (var v in Matches)
                {
                    if (v.Value.AllowNestedDecode)
                    {
                        if (_omitMatches.Contains(v.Value.MatchType))
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

            return _matchesPerIteration;
        }

        /// <summary>
        /// Transform basic markup such as bold, italics, underline, etc. for single and multi-line.
        /// </summary>
        /// <param name="pageContent"></param>
        private void TransformMarkup(WikiString pageContent)
        {
            //ReplaceWholeLineHTMLMarker(pageContent, "**", "strong", true); //Single line bold.
            //ReplaceWholeLineHTMLMarker(pageContent, "__", "u", false); //Single line underline.
            //ReplaceWholeLineHTMLMarker(pageContent, "//", "i", true); //Single line italics.
            //ReplaceWholeLineHTMLMarker(pageContent, "!!", "mark", true); //Single line highlight.

            ReplaceInlineHTMLMarker(pageContent, "~~", "strike", true); //inline bold.
            ReplaceInlineHTMLMarker(pageContent, "**", "strong", true); //inline bold.
            ReplaceInlineHTMLMarker(pageContent, "__", "u", false); //inline highlight.
            ReplaceInlineHTMLMarker(pageContent, "//", "i", true); //inline highlight.
            ReplaceInlineHTMLMarker(pageContent, "!!", "mark", true); //inline highlight.

            var orderedMatches = WikiUtility.OrderMatchesByLengthDescending(
                PrecompiledRegex.TransformHeaderMarkup().Matches(pageContent.ToString()));

            foreach (var match in orderedMatches)
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
                    string value = match.Value.Substring(headingMarkers, match.Value.Length - headingMarkers).Trim();

                    int fontSize = 1 + headingMarkers;
                    if (fontSize < 1) fontSize = 1;

                    string markup = "<font size=\"" + fontSize + "\">" + value + "</font>\r\n";
                    StoreMatch(WikiMatchType.Formatting, pageContent, match.Value, markup);
                }
            }
        }

        /// <summary>
        /// Transform inline and multi-line literal blocks. These are blocks where the content will not be wikified and contain code that is encoded to display verbatim on the page.
        /// </summary>
        /// <param name="pageContent"></param>
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
        /// <param name="pageContent"></param>
        private void TransformScopeFunctions(WikiString pageContent)
        {
            var content = pageContent.ToString();

            string rawBlock = string.Empty;

            while (true)
            {
                int startPos = content.LastIndexOf("{{");
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
        /// <param name="isFirstChance">Only process early functions (like code blocks)</param>
        private void TransformScopeFunctions(WikiString pageContent, bool isFirstChance)
        {
            // {{([\\S\\s]*)}}
            var orderedMatches = WikiUtility.OrderMatchesByLengthDescending(
                PrecompiledRegex.TransformBlock().Matches(pageContent.ToString()));

            var functionHandler = _scopeFunctionHandler;

            foreach (var match in orderedMatches)
            {
                int paramEndIndex = -1;

                FunctionCall function;

                //We are going to mock up a function call:
                string mockFunctionCall = "##" + match.Value.Trim([' ', '\t', '{', '}']);

                try
                {
                    function = FunctionParser.ParseFunctionCall(functionHandler.Prototypes(), mockFunctionCall, out paramEndIndex);

                    var firstChanceFunctions = new string[] { "code" }; //Process these the first time through.
                    if (isFirstChance && firstChanceFunctions.Contains(function.Name.ToLower()) == false)
                    {
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
                    HandleFunctionAndStoreMatch(functionHandler, WikiMatchType.Block, pageContent, match.Value, function, scopeBody);
                }
                catch (Exception ex)
                {
                    StoreError(pageContent, match.Value, ex.Message);
                }
            }
        }

        void HandleFunctionAndStoreMatch(IFunctionHandler handler, WikiMatchType matchType, WikiString pageContent, string matchValue, FunctionCall function, string scopeBody)
        {
            var result = handler.Handle(this, function, scopeBody);

            if (result.Instructions.Contains(HandlerResultInstruction.Skip))
            {
                return;
            }

            bool allowNestedDecode = !result.Instructions.Contains(HandlerResultInstruction.DisallowNestedDecode);

            string identifier;

            if (result.Instructions.Contains(HandlerResultInstruction.OnlyReplaceFirstMatch))
            {
                identifier = StoreFirstMatch(matchType, function, pageContent, matchValue, result.Content, allowNestedDecode);
            }
            else
            {
                identifier = StoreMatch(matchType, pageContent, matchValue, result.Content, allowNestedDecode);
            }

            foreach (var instruction in result.Instructions)
            {
                switch (instruction)
                {
                    case HandlerResultInstruction.KillTrailingLine:
                        pageContent.Replace($"{identifier}\n", $"{identifier}"); //Kill trailing newline.
                        break;
                }
            }
        }

        /// <summary>
        /// Transform headings. These are the basic HTML H1-H6 headings but they are saved for the building of the table of contents.
        /// </summary>
        /// <param name="pageContent"></param>
        void TransformSectionHeadings(WikiString pageContent)
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
                if (headingMarkers >= 2 && headingMarkers <= 6)
                {
                    string tag = _tocName + "_" + TableOfContents.Count().ToString();
                    string value = match.Value.Substring(headingMarkers, match.Value.Length - headingMarkers).Trim().Trim(new char[] { '=' }).Trim();

                    int fontSize = 8 - headingMarkers;
                    if (fontSize < 5) fontSize = 5;

                    string link = "<font size=\"" + fontSize + "\"><a name=\"" + tag + "\"><span class=\"WikiH" + (headingMarkers - 1).ToString() + "\">" + value + "</span></a></font>\r\n";
                    StoreMatch(WikiMatchType.Heading, pageContent, match.Value, link);
                    TableOfContents.Add(new TOCTag(headingMarkers - 1, match.Index, tag, value));
                }
            }
        }

        private string GetLinkImage(List<string> arguments)
        {
            //This function excepts an argument array with up to three arguments:
            //[0] link text.
            //[1] image link, which starts with "img=".
            //[2] scale of image.

            if (arguments.Count < 1 || arguments.Count > 3)
            {
                throw new Exception($"The link parameters are invalid. Expected: [[page, text/image, scale.]], found :[[\"{string.Join("\",\"", arguments)}]]\"");
            }

            var linkText = arguments[1];

            string compareString = Text.RemoveWhitespace(linkText.ToLower());

            //Internal page attached image:
            if (compareString.StartsWith("image="))
            {
                if (linkText.Contains("/"))
                {
                    linkText = linkText.Substring(linkText.IndexOf("=") + 1);

                    if (linkText.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
                    {
                        linkText = $"<img src=\"{linkText}\" border=\"0\" />";
                        return linkText;
                    }

                    string scale = "100";

                    //Allow loading attached images from other pages.
                    int slashIndex = linkText.IndexOf("/");
                    string navigation = NamespaceNavigation.CleanAndValidate(linkText.Substring(0, slashIndex));
                    linkText = linkText.Substring(slashIndex + 1);

                    if (arguments.Count > 2)
                    {
                        scale = arguments[2];
                    }

                    if (Revision != null)
                    {
                        string attachmentLink = $"/Page/Image/{navigation}/{NamespaceNavigation.CleanAndValidate(linkText)}/{Revision}";
                        linkText = $"<img src=\"{attachmentLink}?Scale={scale}\" border=\"0\" />";
                    }
                    else
                    {
                        string attachmentLink = $"/Page/Image/{navigation}/{NamespaceNavigation.CleanAndValidate(linkText)}";
                        linkText = $"<img src=\"{attachmentLink}?Scale={scale}\" border=\"0\" />";
                    }
                }
                else
                {
                    linkText = linkText.Substring(linkText.IndexOf("=") + 1);
                    string scale = "100";

                    if (arguments.Count > 2)
                    {
                        linkText = arguments[1].Substring(arguments[1].IndexOf("=") + 1);
                        scale = arguments[2];
                    }

                    if (Revision != null)
                    {
                        string attachmentLink = $"/Page/Image/{Page.Navigation}/{NamespaceNavigation.CleanAndValidate(linkText)}/{Revision}";
                        linkText = $"<img src=\"{attachmentLink}?Scale={scale}\" border=\"0\" />";
                    }
                    else
                    {
                        string attachmentLink = $"/Page/Image/{Page.Navigation}/{NamespaceNavigation.CleanAndValidate(linkText)}";
                        linkText = $"<img src=\"{attachmentLink}?Scale={scale}\" border=\"0\" />";
                    }
                }
            }
            //External site image:
            else if (compareString.StartsWith("image="))
            {
                linkText = linkText.Substring(linkText.IndexOf("=") + 1);
                linkText = $"<img src=\"{linkText}\" border=\"0\" />";
            }

            return linkText;
        }

        private void TransformComments(WikiString pageContent)
        {
            var orderedMatches = WikiUtility.OrderMatchesByLengthDescending(
                PrecompiledRegex.TransformComments().Matches(pageContent.ToString()));

            foreach (var match in orderedMatches)
            {
                var identifier = StoreMatch(WikiMatchType.Instruction, pageContent, match.Value, "");
                pageContent.Replace($"{identifier}\n", $"{identifier}"); //Kill trailing newline.
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

                key = $"%%{key}%%";

                var emoji = GlobalConfiguration.Emojis.FirstOrDefault(o => o.Shortcut == key);

                if (GlobalConfiguration.Emojis.Exists(o => o.Shortcut == key))
                {
                    if (scale != 100 && scale > 0 && scale <= 500)
                    {
                        var emojiImage = $"<img src=\"/file/Emoji/{key.Trim('%')}?Scale={scale}\" alt=\"{emoji?.Name}\" />";
                        var identifier = StoreMatch(WikiMatchType.Variable, pageContent, match.Value, emojiImage);
                        pageContent.Replace($"{identifier}\n", $"{identifier}"); //Kill trailing newline.
                    }
                    else
                    {
                        var emojiImage = $"<img src=\"/file/Emoji/{key.Trim('%')}\" alt=\"{emoji?.Name}\" />";
                        var identifier = StoreMatch(WikiMatchType.Variable, pageContent, match.Value, emojiImage);
                        pageContent.Replace($"{identifier}\n", $"{identifier}"); //Kill trailing newline.
                    }
                }
                else
                {
                    var identifier = StoreMatch(WikiMatchType.Variable, pageContent, match.Value, match.Value, false);
                    pageContent.Replace($"{identifier}\n", $"{identifier}"); //Kill trailing newline.
                }
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
                string key = match.Value.Trim(new char[] { '{', '}', ' ', '\t', '$' });
                if (key.Contains("="))
                {
                    var sections = key.Split('=');
                    key = sections[0].Trim();
                    var value = sections[1].Trim();

                    if (Variables.ContainsKey(key))
                    {
                        Variables[key] = value;
                    }
                    else
                    {
                        Variables.Add(key, value);
                    }

                    var identifier = StoreMatch(WikiMatchType.Instruction, pageContent, match.Value, "");
                    pageContent.Replace($"{identifier}\n", $"{identifier}"); //Kill trailing newline.
                }
                else
                {
                    if (Variables.ContainsKey(key))
                    {
                        var identifier = StoreMatch(WikiMatchType.Variable, pageContent, match.Value, Variables[key]);
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
                string keyword = match.Value.Substring(2, match.Value.Length - 4).Trim();
                var args = FunctionParser.ParseRawArgumentsAddParenthesis(keyword);

                if (args.Count > 1)
                {
                    string linkText = args[1];
                    if (linkText.StartsWith("image=", StringComparison.CurrentCultureIgnoreCase))
                    {
                        linkText = $"<img {linkText} border =\"0\" > ";
                    }

                    keyword = args[0];

                    StoreMatch(WikiMatchType.Link, pageContent, match.Value, "<a href=\"" + keyword + "\">" + linkText + "</a>", false);
                }
                else
                {
                    StoreMatch(WikiMatchType.Link, pageContent, match.Value, "<a href=\"" + keyword + "\">" + keyword + "</a>", false);
                }
            }

            //Parse external explicit links. eg. [[https://test.net]].
            orderedMatches = WikiUtility.OrderMatchesByLengthDescending(
                PrecompiledRegex.TransformExplicitHTTPsLinks().Matches(pageContent.ToString()));

            foreach (var match in orderedMatches)
            {
                string keyword = match.Value.Substring(2, match.Value.Length - 4).Trim();
                var args = FunctionParser.ParseRawArgumentsAddParenthesis(keyword);

                if (args.Count > 1)
                {
                    string linkText = args[1];
                    if (linkText.StartsWith("image=", StringComparison.CurrentCultureIgnoreCase))
                    {
                        linkText = $"<img {linkText} border =\"0\" > ";
                    }

                    keyword = args[0];

                    StoreMatch(WikiMatchType.Link, pageContent, match.Value, "<a href=\"" + keyword + "\">" + linkText + "</a>", false);
                }
                else
                {
                    StoreMatch(WikiMatchType.Link, pageContent, match.Value, "<a href=\"" + keyword + "\">" + keyword + "</a>", false);
                }
            }

            //Parse internal dynamic links. eg [[AboutUs|About Us]].
            orderedMatches = WikiUtility.OrderMatchesByLengthDescending(
                PrecompiledRegex.TransformInternalDynamicLinks().Matches(pageContent.ToString()));

            foreach (var match in orderedMatches)
            {
                string keyword = match.Value.Substring(2, match.Value.Length - 4);

                bool explicitNamespace = false;

                string explicitLinkText = "";
                string linkText;

                if (keyword.Contains("::"))
                {
                    explicitLinkText = keyword.Substring(keyword.IndexOf("::") + 2).Trim();
                    string ns = keyword.Substring(0, keyword.IndexOf("::")).Trim();
                    explicitNamespace = true;

                    if (ns.IsNullOrEmpty())
                    {
                        //The user explicitly specified an empty namespace, they want this link to go to the root "unnamed" namespace.
                        keyword = keyword.Trim().Trim(':').Trim(); //Trim off the empty namespace name.
                    }
                }

                var args = FunctionParser.ParseRawArgumentsAddParenthesis(keyword);

                if (args.Count == 1)
                {
                    //Text only.
                }
                else if (args.Count >= 2)
                {
                    keyword = args[0];
                    explicitLinkText = args[1];
                }

                string pageName = keyword;
                string pageNavigation = NamespaceNavigation.CleanAndValidate(pageName);
                var page = PageRepository.GetPageRevisionByNavigation(pageNavigation);

                if (page == null && explicitNamespace == false && Page.Namespace != null)
                {
                    if (explicitLinkText.IsNullOrEmpty())
                    {
                        explicitLinkText = keyword;
                    }

                    //If the page does not exist, and no namespace was specified, but the page has a namespace - then default to the pages namespace.
                    if (string.IsNullOrEmpty(Page.Namespace) == false)
                    {
                        pageName = $"{Page.Namespace} :: {keyword}";
                    }

                    pageNavigation = NamespaceNavigation.CleanAndValidate($"{Page.Namespace} :: {pageNavigation}");
                    page = PageRepository.GetPageRevisionByNavigation(pageNavigation);
                }

                OutgoingLinks.Add(new NameNav(pageName, pageNavigation));

                if (page != null)
                {
                    if (explicitLinkText.Length > 0 && explicitLinkText.Contains("image="))
                    {
                        linkText = GetLinkImage(args);
                    }
                    else if (explicitLinkText.Length > 0)
                    {
                        linkText = explicitLinkText;
                    }
                    else
                    {
                        linkText = page.Name;
                    }

                    StoreMatch(WikiMatchType.Link, pageContent, match.Value, "<a href=\"" + NamespaceNavigation.CleanAndValidate($"/{pageNavigation}") + $"\">{linkText}</a>");
                }
                else if (SessionState?.CanCreate == true)
                {
                    if (explicitLinkText.Length > 0)
                    {
                        linkText = explicitLinkText;
                    }
                    else
                    {
                        linkText = pageName;
                    }

                    linkText += "<font color=\"#cc0000\" size=\"2\">?</font>";
                    StoreMatch(WikiMatchType.Link, pageContent, match.Value, "<a href=\"" + NamespaceNavigation.CleanAndValidate($"/{pageNavigation}/Edit/") + $"?Name={pageName}\">{linkText}</a>");
                }
                else
                {
                    if (explicitLinkText.Length > 0)
                    {
                        linkText = explicitLinkText;
                    }
                    else
                    {
                        linkText = pageName;
                    }

                    //Remove wiki tags for pages which were not found or which we do not have permission to view.
                    if (linkText.Length > 0)
                    {
                        StoreMatch(WikiMatchType.Link, pageContent, match.Value, linkText);
                    }
                    else
                    {
                        StoreError(pageContent, match.Value, $"The page has no name for [{keyword}]");
                    }
                }
            }
        }

        /// <summary>
        /// Transform processing instructions are used to flag pages for specific needs such as deletion, review, draft, etc.
        /// </summary>
        /// <param name="pageContent"></param>
        private void TransformProcessingInstructions(WikiString pageContent)
        {
            // <code>(\\@\\@[\\w-]+\\(\\))|(\\@\\@[\\w-]+\\(.*?\\))|(\\@\\@[\\w-]+)</code><br/>
            var orderedMatches = WikiUtility.OrderMatchesByLengthDescending(
                PrecompiledRegex.TransformProcessingInstructions().Matches(pageContent.ToString()));

            var functionHandler = _processingInstructionFunctionHandler;

            foreach (var match in orderedMatches)
            {
                FunctionCall function;

                try
                {
                    function = FunctionParser.ParseFunctionCall(functionHandler.Prototypes(), match.Value, out int matchEndIndex);
                }
                catch (Exception ex)
                {
                    StoreError(pageContent, match.Value, ex.Message);
                    continue;
                }

                try
                {
                    HandleFunctionAndStoreMatch(functionHandler, WikiMatchType.Instruction, pageContent, match.Value, function, string.Empty);
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

            var functionHandler = _standardFunctionHandler;

            foreach (var match in orderedMatches)
            {
                FunctionCall function;

                try
                {
                    function = FunctionParser.ParseFunctionCall(functionHandler.Prototypes(), match.Value, out int matchEndIndex);
                }
                catch (Exception ex)
                {
                    //TODO: We blow up here because the TOC function is not a standard function, but rather a post processing instruction.
                    //          We need to be able to handle this without simply skipping it because skipping could cause infinite looping.
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
                    HandleFunctionAndStoreMatch(functionHandler, WikiMatchType.Function, pageContent, match.Value, function, string.Empty);
                }
                catch (Exception ex)
                {
                    StoreError(pageContent, match.Value, ex.Message);
                }
            }
        }

        /// <summary>
        /// Transform post-process are functions that must be called after all other transformations. For example, we can't build a table-of-contents until we have parsed the entire page.
        /// </summary>
        private void TransformPostProcess(WikiString pageContent)
        {
            //Remove the last "(\#\#[\w-]+)" if you start to have matching problems:
            var orderedMatches = WikiUtility.OrderMatchesByLengthDescending(
                PrecompiledRegex.TransformPostProcess().Matches(pageContent.ToString()));

            var functionHandler = _standardPostProcessingFunctionHandler;

            foreach (var match in orderedMatches)
            {
                FunctionCall function;

                try
                {
                    function = FunctionParser.ParseFunctionCall(functionHandler.Prototypes(), match.Value, out int matchEndIndex);
                }
                catch (Exception ex)
                {
                    StoreError(pageContent, match.Value, ex.Message);
                    continue;
                }

                try
                {
                    HandleFunctionAndStoreMatch(functionHandler, WikiMatchType.Function, pageContent, match.Value, function, string.Empty);
                }
                catch (Exception ex)
                {
                    StoreError(pageContent, match.Value, ex.Message);
                }
            }
        }

        private void StoreCriticalError(Exception ex)
        {
            ExceptionRepository.InsertException(ex, $"Page: {Page.Navigation}, Error: {ex.Message}");

            ErrorCount++;
            ProcessedBody = WikiUtility.WarningCard("Wiki Parser Exception", ex.Message);
        }

        private string StoreError(WikiString pageContent, string match, string value)
        {
            ExceptionRepository.InsertException($"Page: {Page.Navigation}, Error: {value}");

            ErrorCount++;
            _matchesPerIteration++;

            string identifier = $"<!--{Guid.NewGuid()}-->";

            var matchSet = new MatchSet()
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
            _matchesPerIteration++;

            string identifier = $"<!--{Guid.NewGuid()}-->";

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

        private string StoreMatch(FunctionCall function, WikiString pageContent, string match, string value, bool allowNestedDecode = true)
        {
            MatchCount++;
            _matchesPerIteration++;

            string identifier = $"<!--{Guid.NewGuid()}-->";

            var matchSet = new MatchSet()
            {
                MatchType = WikiMatchType.Function,
                Content = value,
                Function = function,
                AllowNestedDecode = allowNestedDecode
            };

            Matches.Add(identifier, matchSet);
            pageContent.Replace(match, identifier);

            return identifier;
        }

        private string StoreFirstMatch(WikiMatchType matchType, FunctionCall function, WikiString pageContent, string match, string value, bool allowNestedDecode = true)
        {
            MatchCount++;
            _matchesPerIteration++;

            string identifier = $"<!--{Guid.NewGuid()}-->";

            var matchSet = new MatchSet()
            {
                MatchType = matchType,
                Content = value,
                Function = function,
                AllowNestedDecode = allowNestedDecode
            };
            Matches.Add(identifier, matchSet);

            var pageContentCopy = Text.ReplaceFirstOccurrence(pageContent.ToString(), match, identifier);
            pageContent.Clear();
            pageContent.Append(pageContentCopy);

            return identifier;
        }

        private void TransformWhitespace(WikiString pageContent)
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

        /// <summary>
        /// Replaces HTML where we are transforming the entire line, such as "*this will be bold" - > "<b>this will be bold</b>
        /// </summary>
        /// <param name="regex"></param>
        /// <param name="htmlTag"></param>
        void ReplaceWholeLineHTMLMarker(WikiString pageContent, string mark, string htmlTag, bool escape)
        {
            string marker = string.Empty;
            if (escape)
            {
                foreach (var c in mark)
                {
                    marker += $"\\{c}";
                }
            }
            else
            {
                marker = mark;
            }

            var rgx = new Regex($"^{marker}.*?\n", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            var orderedMatches = WikiUtility.OrderMatchesByLengthDescending(rgx.Matches(pageContent.ToString()));
            //We roll-through these matches in reverse order because we are replacing by position. We don't move the earlier positions by replacing from the bottom up.
            foreach (var match in orderedMatches)
            {
                string value = match.Value.Substring(mark.Length, match.Value.Length - mark.Length).Trim();
                var matchString = match.Value.Trim(); //We trim the match because we are matching to the end of the line which includes the \r\n, which we do not want to replace.
                StoreMatch(WikiMatchType.Formatting, pageContent, matchString, $"<{htmlTag}>{value}</{htmlTag}> ");
            }
        }

        /// <summary>
        /// Used to generate unique and regenerable query string tokens for page links.
        /// </summary>
        /// <returns></returns>
        public string GenerateQueryToken()
        {
            _queryTokenState = Security.Helpers.Sha256(Security.Helpers.EncryptString(_queryTokenState, _queryTokenState));
            return $"H{Security.Helpers.Crc32(_queryTokenState)}";
        }

        void ReplaceInlineHTMLMarker(WikiString pageContent, string mark, string htmlTag, bool escape)
        {
            string marker = string.Empty;
            if (escape)
            {
                foreach (var c in mark)
                {
                    marker += $"\\{c}";
                }
            }
            else
            {
                marker = mark;
            }

            var rgx = new Regex(@$"{marker}([^\/\n\r]*){marker}", RegexOptions.IgnoreCase);
            var orderedMatches = WikiUtility.OrderMatchesByLengthDescending(rgx.Matches(pageContent.ToString()));
            foreach (var match in orderedMatches)
            {
                string markup = match.Value.Substring(mark.Length, match.Value.Length - mark.Length * 2);

                StoreMatch(WikiMatchType.Formatting, pageContent, match.Value, $"<{htmlTag}>{markup}</{htmlTag}>");
            }
        }
    }
}
