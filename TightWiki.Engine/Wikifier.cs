using DuoVia.FuzzyStrings;
using Microsoft.AspNetCore.Http;
using NTDLS.Helpers;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using TightWiki.Configuration;
using TightWiki.Engine.Implementation;
using TightWiki.Engine.Types;
using TightWiki.EngineFunction;
using TightWiki.Library;
using TightWiki.Library.Interfaces;
using TightWiki.Models;
using TightWiki.Models.DataModels;
using TightWiki.Repository;
using static TightWiki.Engine.Types.Constants;
using static TightWiki.Library.Constants;

namespace TightWiki.Engine
{
    public partial class Wikifier
    {
        public int ErrorCount { get; private set; }
        public int MatchCount { get; private set; }
        public TimeSpan ProcessingTime { get; private set; }

        private const string SoftBreak = "<!--SoftBreak-->"; //These will remain as \r\n in the final HTML.
        private const string HardBreak = "<!--HardBreak-->"; //These will remain as <br /> in the final HTML.

        public readonly Dictionary<string, string> UserVariables = new();
        public readonly Dictionary<string, string> Snippets = new();
        public List<NameNav> OutgoingLinks { get; private set; } = new();
        public List<string> ProcessingInstructions { get; private set; } = new();
        public string ProcessedBody { get; private set; } = string.Empty;
        public List<string> Tags { get; private set; } = new();
        public Dictionary<string, MatchSet> Matches { get; private set; } = new();

        private readonly Dictionary<string, int> _sequences = new();
        private string _queryTokenState = Security.Helpers.MachineKey;
        private int _matchesPerIteration = 0;
        private readonly string _tocName = "TOC_" + new Random().Next(0, 1000000).ToString();
        private readonly List<TOCTag> _tocTags = new();
        private readonly Page _page;
        private readonly int? _revision;
        private readonly IQueryCollection _queryString;
        private readonly ISessionState? _sessionState;
        private readonly int _nestLevel;
        private readonly HashSet<WikiMatchType> _omitMatches = new();

        public Wikifier(ISessionState? sessionState, Page page, int? revision = null,
             WikiMatchType[]? omitMatches = null, int nestLevel = 0)
        {
            DateTime startTime = DateTime.UtcNow;

            _nestLevel = nestLevel;
            _queryString = sessionState?.QueryString ?? new QueryCollection();
            _page = page;
            _revision = revision;
            Matches = new Dictionary<string, MatchSet>();
            _sessionState = sessionState;

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

        public List<WeightedToken> ParsePageTokens()
        {
            var allTokens = new List<WeightedToken>();

            allTokens.AddRange(WikiUtility.ParsePageTokens(ProcessedBody, 1));
            allTokens.AddRange(WikiUtility.ParsePageTokens(_page.Description, 1.2));
            allTokens.AddRange(WikiUtility.ParsePageTokens(string.Join(" ", Tags), 1.4));
            allTokens.AddRange(WikiUtility.ParsePageTokens(_page.Name, 1.6));

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
            var pageContent = new WikiString(_page.Body);

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

            ProcessedBody = pageContent.ToString();
        }

        public int TransformAll(WikiString pageContent)
        {
            _matchesPerIteration = 0;

            TransformComments(pageContent);
            TransformSectionHeadings(pageContent);
            TransformBlocks(pageContent);
            TransformVariables(pageContent);
            TransformLinks(pageContent);
            TransformMarkup(pageContent);
            TransformEmoji(pageContent);
            TransformFunctions(pageContent, true);
            TransformFunctions(pageContent, false);
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
        /// <param name="firstBlocks">Only process early functions (like code blocks)</param>
        private void TransformBlocks(WikiString pageContent)
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
                TransformBlock(transformBlock, true);
                TransformBlock(transformBlock, false);
                content = content.Replace(rawBlock, transformBlock.ToString());
            }

            pageContent.Clear();
            pageContent.Append(content);
        }

        /// <summary>
        /// Transform blocks or sections of code, these are thinks like panels and alerts.
        /// </summary>
        /// <param name="pageContent"></param>
        /// <param name="firstBlocks">Only process early functions (like code blocks)</param>
        private void TransformBlock(WikiString pageContent, bool firstBlocks)
        {
            var orderedMatches = WikiUtility.OrderMatchesByLengthDescending(
                PrecompiledRegex.TransformBlock().Matches(pageContent.ToString()));

            foreach (var match in orderedMatches)
            {
                int paramEndIndex = -1;

                FunctionCall function;

                //We are going to mock up a function call:
                var originalMatchValue = match.Value;
                match.Value = "##" + match.Value.Trim(new char[] { ' ', '\t', '{', '}' });

                try
                {
                    function = FunctionParser.ParseFunctionCall(match.Value, out paramEndIndex);
                }
                catch (Exception ex)
                {
                    StoreError(pageContent, originalMatchValue, ex.Message);
                    continue;
                }

                string scopeBody = match.Value.Substring(paramEndIndex).Trim();

                var html = new StringBuilder();

                bool allowNestedDecode = true;

                if (firstBlocks == true)//Process early blocks, like code because they need to be processed first.
                {
                    switch (function.Name.ToLower())
                    {
                        //------------------------------------------------------------------------------------------------------------------------------
                        case "code":
                            {
                                string language = function.Parameters.Get<string>("language");
                                if (string.IsNullOrEmpty(language) || language?.ToLower() == "auto")
                                {
                                    html.Append($"<pre>");
                                    html.Append($"<code>{scopeBody.Replace("\r\n", "\n").Replace("\n", SoftBreak)}</code></pre>");
                                }
                                else
                                {
                                    html.Append($"<pre class=\"language-{language}\">");
                                    html.Append($"<code>{scopeBody.Replace("\r\n", "\n").Replace("\n", SoftBreak)}</code></pre>");
                                }
                                allowNestedDecode = false;
                            }
                            break;
                        default:
                            continue; //We don't want to replace tags that we cant match because we are only partially matching the possible functions with earlyt matching.
                    }
                    StoreMatch(WikiMatchType.Block, pageContent, originalMatchValue, html.ToString(), allowNestedDecode);
                }
                else if (firstBlocks == false)
                {
                    switch (function.Name.ToLower())
                    {
                        //------------------------------------------------------------------------------------------------------------------------------
                        case "stripedtable":
                        case "table":
                            {
                                var hasBorder = function.Parameters.Get<bool>("hasBorder");
                                var isFirstRowHeader = function.Parameters.Get<bool>("isFirstRowHeader");

                                html.Append($"<table class=\"table");

                                if (function.Name.ToLower() == "stripedtable")
                                {
                                    html.Append(" table-striped");
                                }
                                if (hasBorder)
                                {
                                    html.Append(" table-bordered");
                                }

                                html.Append($"\">");

                                var lines = scopeBody.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(o => o.Trim()).Where(o => o.Length > 0);

                                int rowNumber = 0;

                                foreach (var lineText in lines)
                                {
                                    var columns = lineText.Split("||");

                                    if (rowNumber == 0 && isFirstRowHeader)
                                    {
                                        html.Append($"<thead>");
                                    }
                                    else if (rowNumber == 1 && isFirstRowHeader || rowNumber == 0 && isFirstRowHeader == false)
                                    {
                                        html.Append($"<tbody>");
                                    }

                                    html.Append($"<tr>");
                                    foreach (var columnText in columns)
                                    {
                                        if (rowNumber == 0 && isFirstRowHeader)
                                        {
                                            html.Append($"<td><strong>{columnText}</strong></td>");
                                        }
                                        else
                                        {
                                            html.Append($"<td>{columnText}</td>");
                                        }
                                    }

                                    if (rowNumber == 0 && isFirstRowHeader)
                                    {
                                        html.Append($"</thead>");
                                    }
                                    html.Append($"</tr>");

                                    rowNumber++;
                                }

                                html.Append($"</tbody>");
                                html.Append($"</table>");

                                break;
                            }
                        //------------------------------------------------------------------------------------------------------------------------------
                        case "bullets":
                            {
                                string type = function.Parameters.Get<string>("type");

                                if (type == "unordered")
                                {
                                    var lines = scopeBody.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(o => o.Trim()).Where(o => o.Length > 0);

                                    int currentLevel = 0;

                                    foreach (var line in lines)
                                    {
                                        int newIndent = WikiUtility.StartsWithHowMany(line, '>') + 1;

                                        if (newIndent < currentLevel)
                                        {
                                            for (; currentLevel != newIndent; currentLevel--)
                                            {
                                                html.Append($"</ul>");
                                            }
                                        }
                                        else if (newIndent > currentLevel)
                                        {
                                            for (; currentLevel != newIndent; currentLevel++)
                                            {
                                                html.Append($"<ul>");
                                            }
                                        }

                                        html.Append($"<li>{line.Trim(new char[] { '>' })}</li>");
                                    }

                                    for (; currentLevel > 0; currentLevel--)
                                    {
                                        html.Append($"</ul>");
                                    }
                                }
                                else if (type == "ordered")
                                {
                                    var lines = scopeBody.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(o => o.Trim()).Where(o => o.Length > 0);

                                    int currentLevel = 0;

                                    foreach (var line in lines)
                                    {
                                        int newIndent = WikiUtility.StartsWithHowMany(line, '>') + 1;

                                        if (newIndent < currentLevel)
                                        {
                                            for (; currentLevel != newIndent; currentLevel--)
                                            {
                                                html.Append($"</ol>");
                                            }
                                        }
                                        else if (newIndent > currentLevel)
                                        {
                                            for (; currentLevel != newIndent; currentLevel++)
                                            {
                                                html.Append($"<ol>");
                                            }
                                        }

                                        html.Append($"<li>{line.Trim(new char[] { '>' })}</li>");
                                    }

                                    for (; currentLevel > 0; currentLevel--)
                                    {
                                        html.Append($"</ol>");
                                    }
                                }
                            }
                            break;
                        //------------------------------------------------------------------------------------------------------------------------------
                        case "definesnippet":
                            {
                                string name = function.Parameters.Get<string>("name");

                                if (Snippets.ContainsKey(name))
                                {
                                    Snippets[name] = scopeBody;
                                }
                                else
                                {
                                    Snippets.Add(name, scopeBody);
                                }
                            }
                            break;
                        //------------------------------------------------------------------------------------------------------------------------------
                        case "alert":
                            {
                                string titleText = function.Parameters.Get<string>("titleText");
                                string style = function.Parameters.Get<string>("styleName").ToLower();
                                style = style == "default" ? "" : $"alert-{style}";

                                if (!string.IsNullOrEmpty(titleText)) scopeBody = $"<h1>{titleText}</h1>{scopeBody}";
                                html.Append($"<div class=\"alert {style}\">{scopeBody}</div>");
                            }
                            break;

                        case "order":
                            {
                                string direction = function.Parameters.Get<string>("direction");
                                var lines = scopeBody.Split("\n").Select(o => o.Trim()).ToList();

                                if (direction == "ascending")
                                {
                                    html.Append(string.Join("\r\n", lines.OrderBy(o => o)));
                                }
                                else
                                {
                                    html.Append(string.Join("\r\n", lines.OrderByDescending(o => o)));
                                }
                            }
                            break;

                        //------------------------------------------------------------------------------------------------------------------------------
                        case "jumbotron":
                            {
                                string titleText = function.Parameters.Get("titleText", "");
                                html.Append($"<div class=\"mt-4 p-5 bg-secondary text-white rounded\">");
                                if (!string.IsNullOrEmpty(titleText)) html.Append($"<h1>{titleText}</h1>");
                                html.Append($"<p>{scopeBody}</p>");
                                html.Append($"</div>");
                            }
                            break;
                        //------------------------------------------------------------------------------------------------------------------------------
                        case "foreground":
                            {
                                var style = WikiUtility.GetForegroundStyle(function.Parameters.Get("styleName", "default")).Swap();
                                html.Append($"<p class=\"{style.ForegroundStyle} {style.BackgroundStyle}\">{scopeBody}</p>");
                            }
                            break;
                        //------------------------------------------------------------------------------------------------------------------------------
                        case "background":
                            {
                                var style = WikiUtility.GetBackgroundStyle(function.Parameters.Get("styleName", "default"));
                                html.Append($"<div class=\"p-3 mb-2 {style.ForegroundStyle} {style.BackgroundStyle}\">{scopeBody}</div>");
                            }
                            break;
                        //------------------------------------------------------------------------------------------------------------------------------
                        case "collapse":
                            {
                                string linkText = function.Parameters.Get<string>("linktext");
                                string uid = "A" + Guid.NewGuid().ToString().Replace("-", "");
                                html.Append($"<a data-bs-toggle=\"collapse\" href=\"#{uid}\" role=\"button\" aria-expanded=\"false\" aria-controls=\"{uid}\">{linkText}</a>");
                                html.Append($"<div class=\"collapse\" id=\"{uid}\">");
                                html.Append($"<div class=\"card card-body\"><p class=\"card-text\">{scopeBody}</p></div></div>");
                            }
                            break;
                        //------------------------------------------------------------------------------------------------------------------------------
                        case "callout":
                            {
                                string titleText = function.Parameters.Get<string>("titleText");
                                string style = function.Parameters.Get<string>("styleName").ToLower();
                                style = style == "default" ? "" : style;

                                html.Append($"<div class=\"bd-callout bd-callout-{style}\">");
                                if (string.IsNullOrWhiteSpace(titleText) == false) html.Append($"<h4>{titleText}</h4>");
                                html.Append($"{scopeBody}");
                                html.Append($"</div>");
                            }
                            break;
                        //------------------------------------------------------------------------------------------------------------------------------
                        case "card":
                            {
                                string titleText = function.Parameters.Get<string>("titleText");
                                var style = WikiUtility.GetBackgroundStyle(function.Parameters.Get("styleName", "default"));

                                html.Append($"<div class=\"card {style.ForegroundStyle} {style.BackgroundStyle} mb-3\">");
                                if (string.IsNullOrEmpty(titleText) == false)
                                {
                                    html.Append($"<div class=\"card-header\">{titleText}</div>");
                                }
                                html.Append("<div class=\"card-body\">");
                                html.Append($"<p class=\"card-text\">{scopeBody}</p>");
                                html.Append("</div>");
                                html.Append("</div>");
                            }
                            break;
                    }
                    StoreMatch(WikiMatchType.Block, pageContent, originalMatchValue, html.ToString(), allowNestedDecode);
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
                    string tag = _tocName + "_" + _tocTags.Count().ToString();
                    string value = match.Value.Substring(headingMarkers, match.Value.Length - headingMarkers).Trim().Trim(new char[] { '=' }).Trim();

                    int fontSize = 8 - headingMarkers;
                    if (fontSize < 5) fontSize = 5;

                    string link = "<font size=\"" + fontSize + "\"><a name=\"" + tag + "\"><span class=\"WikiH" + (headingMarkers - 1).ToString() + "\">" + value + "</span></a></font>\r\n";
                    StoreMatch(WikiMatchType.Heading, pageContent, match.Value, link);
                    _tocTags.Add(new TOCTag(headingMarkers - 1, match.Index, tag, value));
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

                    if (_revision != null)
                    {
                        string attachmentLink = $"/Page/Image/{navigation}/{NamespaceNavigation.CleanAndValidate(linkText)}/{_revision}";
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

                    if (_revision != null)
                    {
                        string attachmentLink = $"/Page/Image/{_page.Navigation}/{NamespaceNavigation.CleanAndValidate(linkText)}/{_revision}";
                        linkText = $"<img src=\"{attachmentLink}?Scale={scale}\" border=\"0\" />";
                    }
                    else
                    {
                        string attachmentLink = $"/Page/Image/{_page.Navigation}/{NamespaceNavigation.CleanAndValidate(linkText)}";
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

                    if (UserVariables.ContainsKey(key))
                    {
                        UserVariables[key] = value;
                    }
                    else
                    {
                        UserVariables.Add(key, value);
                    }

                    var identifier = StoreMatch(WikiMatchType.Instruction, pageContent, match.Value, "");
                    pageContent.Replace($"{identifier}\n", $"{identifier}"); //Kill trailing newline.
                }
                else
                {
                    if (UserVariables.ContainsKey(key))
                    {
                        var identifier = StoreMatch(WikiMatchType.Variable, pageContent, match.Value, UserVariables[key]);
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

                if (page == null && explicitNamespace == false && _page.Namespace != null)
                {
                    if (explicitLinkText.IsNullOrEmpty())
                    {
                        explicitLinkText = keyword;
                    }

                    //If the page does not exist, and no namespace was specified, but the page has a namespace - then default to the pages namespace.
                    if (string.IsNullOrEmpty(_page.Namespace) == false)
                    {
                        pageName = $"{_page.Namespace} :: {keyword}";
                    }

                    pageNavigation = NamespaceNavigation.CleanAndValidate($"{_page.Namespace} :: {pageNavigation}");
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
                else if (_sessionState?.CanCreate == true)
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
            var orderedMatches = WikiUtility.OrderMatchesByLengthDescending(
                PrecompiledRegex.TransformProcessingInstructions().Matches(pageContent.ToString()));

            foreach (var match in orderedMatches)
            {
                FunctionCall function;

                try
                {
                    function = FunctionParser.ParseFunctionCall(match.Value, out int matchEndIndex);
                }
                catch (Exception ex)
                {
                    StoreError(pageContent, match.Value, ex.Message);
                    continue;
                }

                switch (function.Name.ToLower())
                {
                    //We check _nestLevel here because we don't want to include the processing instructions on any parent pages that are injecting this one.

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

                            string category = _queryString["Category"].ToString();

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


                            var identifier = StoreMatch(WikiMatchType.Instruction, pageContent, match.Value, html.ToString(), false);
                        }
                        break;
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


                            var identifier = StoreMatch(WikiMatchType.Instruction, pageContent, match.Value, html.ToString(), false);
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "hidefooterlastmodified":
                        {
                            ProcessingInstructions.Add(WikiInstruction.HideFooterLastModified);
                            var identifier = StoreMatch(WikiMatchType.Instruction, pageContent, match.Value, "");
                            pageContent.Replace($"{identifier}\n", $"{identifier}"); //Kill trailing newline.
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "hidefootercomments":
                        {
                            ProcessingInstructions.Add(WikiInstruction.HideFooterComments);
                            var identifier = StoreMatch(WikiMatchType.Instruction, pageContent, match.Value, "");
                            pageContent.Replace($"{identifier}\n", $"{identifier}"); //Kill trailing newline.
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "nocache":
                        {
                            ProcessingInstructions.Add(WikiInstruction.NoCache);
                            var identifier = StoreMatch(WikiMatchType.Instruction, pageContent, match.Value, "");
                            pageContent.Replace($"{identifier}\n", $"{identifier}"); //Kill trailing newline.
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "deprecate":
                        {
                            if (_nestLevel == 0)
                            {
                                ProcessingInstructions.Add(WikiInstruction.Deprecate);
                                pageContent.Insert(0, "<div class=\"alert alert-danger\">This page has been deprecated and will eventually be deleted.</div>");
                            }
                            var identifier = StoreMatch(WikiMatchType.Instruction, pageContent, match.Value, "");
                            pageContent.Replace($"{identifier}\n", $"{identifier}"); //Kill trailing newline.
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "protect":
                        {
                            if (_nestLevel == 0)
                            {
                                bool isSilent = function.Parameters.Get<bool>("isSilent");
                                ProcessingInstructions.Add(WikiInstruction.Protect);
                                if (isSilent == false)
                                {
                                    pageContent.Insert(0, "<div class=\"alert alert-info\">This page has been protected and can not be changed by non-moderators.</div>");
                                }
                            }
                            var identifier = StoreMatch(WikiMatchType.Instruction, pageContent, match.Value, "");
                            pageContent.Replace($"{identifier}\n", $"{identifier}"); //Kill trailing newline.
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "template":
                        {
                            if (_nestLevel == 0)
                            {
                                ProcessingInstructions.Add(WikiInstruction.Template);
                                pageContent.Insert(0, "<div class=\"alert alert-secondary\">This page is a template and will not appear in indexes or glossaries.</div>");
                            }
                            var identifier = StoreMatch(WikiMatchType.Instruction, pageContent, match.Value, "");
                            pageContent.Replace($"{identifier}\n", $"{identifier}"); //Kill trailing newline.
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "review":
                        {
                            if (_nestLevel == 0)
                            {
                                ProcessingInstructions.Add(WikiInstruction.Review);
                                pageContent.Insert(0, "<div class=\"alert alert-warning\">This page has been flagged for review, its content may be inaccurate.</div>");
                            }
                            var identifier = StoreMatch(WikiMatchType.Instruction, pageContent, match.Value, "");
                            pageContent.Replace($"{identifier}\n", $"{identifier}"); //Kill trailing newline.
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "include":
                        {
                            if (_nestLevel == 0)
                            {
                                ProcessingInstructions.Add(WikiInstruction.Include);
                                pageContent.Insert(0, "<div class=\"alert alert-secondary\">This page is an include and will not appear in indexes or glossaries.</div>");
                            }
                            var identifier = StoreMatch(WikiMatchType.Instruction, pageContent, match.Value, "");
                            pageContent.Replace($"{identifier}\n", $"{identifier}"); //Kill trailing newline.
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "draft":
                        {
                            if (_nestLevel == 0)
                            {
                                ProcessingInstructions.Add(WikiInstruction.Draft);
                                pageContent.Insert(0, "<div class=\"alert alert-warning\">This page is a draft and may contain incorrect information and/or experimental styling.</div>");
                            }
                            var identifier = StoreMatch(WikiMatchType.Instruction, pageContent, match.Value, "");
                            pageContent.Replace($"{identifier}\n", $"{identifier}"); //Kill trailing newline.
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Transform functions is used to call wiki functions such as including template pages, setting tags and displaying images.
        /// </summary>
        /// <param name="pageContent"></param>
        private void TransformFunctions(WikiString pageContent, bool isFirstChance)
        {
            //Remove the last "(\#\#[\w-]+)" if you start to have matching problems:
            var orderedMatches = WikiUtility.OrderMatchesByLengthDescending(
                PrecompiledRegex.TransformFunctions().Matches(pageContent.ToString()));

            foreach (var match in orderedMatches)
            {
                FunctionCall function;

                try
                {
                    function = FunctionParser.ParseFunctionCall(match.Value, out int matchEndIndex);
                }
                catch (Exception ex)
                {
                    StoreError(pageContent, match.Value, ex.Message);
                    continue;
                }

                var firstChanceFunctions = new string[] { "include", "inject" }; //Process these the first time though.
                if (isFirstChance && firstChanceFunctions.Contains(function.Name.ToLower()) == false)
                {
                    continue;
                }

                switch (function.Name.ToLower())
                {
                    //------------------------------------------------------------------------------------------------------------------------------
                    //Creates a glossary all user profiles.
                    case "profileglossary":
                        {
                            var html = new StringBuilder();
                            string refTag = GenerateQueryToken();
                            int pageNumber = int.Parse(_queryString[refTag].ToString().DefaultWhenNullOrEmpty("1"));
                            var pageSize = function.Parameters.Get<int>("pageSize");
                            var searchToken = function.Parameters.Get<string>("searchToken");
                            var topCount = function.Parameters.Get<int>("top");
                            var profiles = UsersRepository.GetAllPublicProfilesPaged(pageNumber, pageSize, searchToken);

                            string glossaryName = "glossary_" + new Random().Next(0, 1000000).ToString();
                            var alphabet = profiles.Select(p => p.AccountName.Substring(0, 1).ToUpper()).Distinct();

                            if (profiles.Count() > 0)
                            {
                                html.Append("<center>");
                                foreach (var alpha in alphabet)
                                {
                                    html.Append("<a href=\"#" + glossaryName + "_" + alpha + "\">" + alpha + "</a>&nbsp;");
                                }
                                html.Append("</center>");

                                html.Append("<ul>");
                                foreach (var alpha in alphabet)
                                {
                                    html.Append("<li><a name=\"" + glossaryName + "_" + alpha + "\">" + alpha + "</a></li>");

                                    html.Append("<ul>");
                                    foreach (var profile in profiles.Where(p => p.AccountName.ToLower().StartsWith(alpha.ToLower())))
                                    {
                                        html.Append($"<li><a href=\"/Profile/{profile.Navigation}/Public\">{profile.AccountName}</a>");
                                        html.Append("</li>");
                                    }
                                    html.Append("</ul>");
                                }

                                html.Append("</ul>");
                            }
                            StoreMatch(function, pageContent, match.Value, html.ToString());
                        }
                        break;


                    //------------------------------------------------------------------------------------------------------------------------------
                    //Creates a list of all user profiles.
                    case "profilelist":
                        {
                            var html = new StringBuilder();
                            string refTag = GenerateQueryToken();
                            int pageNumber = int.Parse(_queryString[refTag].ToString().DefaultWhenNullOrEmpty("1"));
                            var pageSize = function.Parameters.Get<int>("pageSize");
                            var searchToken = function.Parameters.Get<string>("searchToken");
                            var profiles = UsersRepository.GetAllPublicProfilesPaged(pageNumber, pageSize, searchToken);

                            if (profiles.Count() > 0)
                            {
                                html.Append("<ul>");

                                foreach (var profile in profiles)
                                {
                                    html.Append($"<li><a href=\"/Profile/{profile.Navigation}/Public\">{profile.AccountName}</a>");
                                    html.Append("</li>");
                                }

                                html.Append("</ul>");
                            }

                            if (profiles.Count > 0 && profiles.First().PaginationPageCount > 1)
                            {
                                html.Append(PageSelectorGenerator.Generate(refTag, _queryString, profiles.First().PaginationPageCount));
                            }

                            StoreMatch(function, pageContent, match.Value, html.ToString());
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    case "attachments":
                        {
                            string refTag = GenerateQueryToken();

                            int pageNumber = int.Parse(_queryString[refTag].ToString().DefaultWhenNullOrEmpty("1"));

                            var navigation = NamespaceNavigation.CleanAndValidate(function.Parameters.Get("pageName", _page.Navigation));
                            string styleName = function.Parameters.Get<string>("styleName").ToLower();
                            var pageSize = function.Parameters.Get<int>("pageSize");
                            var pageSelector = function.Parameters.Get<bool>("pageSelector");
                            var attachments = PageFileRepository.GetPageFilesInfoByPageNavigationAndPageRevisionPaged(navigation, pageNumber, pageSize, _revision);
                            var html = new StringBuilder();

                            if (attachments.Count() > 0)
                            {
                                html.Append("<ul>");
                                foreach (var file in attachments)
                                {
                                    if (_revision != null)
                                    {
                                        html.Append($"<li><a href=\"/Page/Binary/{_page.Navigation}/{file.FileNavigation}/{_revision}\">{file.Name}</a>");
                                    }
                                    else
                                    {
                                        html.Append($"<li><a href=\"/Page/Binary/{_page.Navigation}/{file.FileNavigation}\">{file.Name} </a>");
                                    }

                                    if (styleName == "full")
                                    {
                                        html.Append($" - ({file.FriendlySize})");
                                    }

                                    html.Append("</li>");
                                }
                                html.Append("</ul>");

                                if (pageSelector && attachments.Count > 0 && attachments.First().PaginationPageCount > 1)
                                {
                                    html.Append(PageSelectorGenerator.Generate(refTag, _queryString, attachments.First().PaginationPageCount));
                                }
                            }

                            StoreMatch(function, pageContent, match.Value, html.ToString());
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "revisions":
                        {
                            if (_sessionState == null)
                            {
                                StoreError(pageContent, match.Value, $"Localization is not supported without SessionState.");
                                continue;
                            }

                            string refTag = GenerateQueryToken();

                            int pageNumber = int.Parse(_queryString[refTag].ToString().DefaultWhenNullOrEmpty("1"));

                            var navigation = NamespaceNavigation.CleanAndValidate(function.Parameters.Get("pageName", _page.Navigation));
                            string styleName = function.Parameters.Get<string>("styleName").ToLower();
                            var pageSize = function.Parameters.Get<int>("pageSize");
                            var pageSelector = function.Parameters.Get<bool>("pageSelector");
                            var revisions = PageRepository.GetPageRevisionsInfoByNavigationPaged(navigation, pageNumber, null, null, pageSize);
                            var html = new StringBuilder();

                            if (revisions.Count() > 0)
                            {
                                html.Append("<ul>");
                                foreach (var item in revisions)
                                {
                                    html.Append($"<li><a href=\"/{item.Navigation}/{item.Revision}\">{item.Revision} by {item.ModifiedByUserName} on {_sessionState.LocalizeDateTime(item.ModifiedDate)}</a>");

                                    if (styleName == "full")
                                    {
                                        var thisRev = PageRepository.GetPageRevisionByNavigation(_page.Navigation, item.Revision);
                                        var prevRev = PageRepository.GetPageRevisionByNavigation(_page.Navigation, item.Revision - 1);

                                        var summaryText = Differentiator.GetComparisonSummary(thisRev?.Body ?? string.Empty, prevRev?.Body ?? string.Empty);

                                        if (summaryText.Length > 0)
                                        {
                                            html.Append(" - " + summaryText);
                                        }
                                    }
                                    html.Append("</li>");
                                }
                                html.Append("</ul>");

                                if (pageSelector && revisions.Count > 0 && revisions.First().PaginationPageCount > 1)
                                {
                                    html.Append(PageSelectorGenerator.Generate(refTag, _queryString, revisions.First().PaginationPageCount));
                                }
                            }

                            StoreMatch(function, pageContent, match.Value, html.ToString());
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "seq": //##Seq({Key})   Inserts a sequence into the document.
                        {
                            var key = function.Parameters.Get<string>("Key");

                            if (_sequences.ContainsKey(key) == false)
                            {
                                _sequences.Add(key, 0);
                            }

                            _sequences[key]++;

                            StoreFirstMatch(function, pageContent, match.Value, _sequences[key].ToString());
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "editlink": //(##EditLink(link text))
                        {
                            var linkText = function.Parameters.Get<string>("linkText");
                            StoreMatch(function, pageContent, match.Value, "<a href=\"" + NamespaceNavigation.CleanAndValidate($"/{_page.Navigation}/Edit") + $"\">{linkText}</a>");
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    //injects an un-processed wiki body into the calling page.
                    case "inject": //(PageName)
                        {
                            var navigation = function.Parameters.Get<string>("pageName");

                            var page = WikiUtility.GetPageFromPathInfo(navigation);
                            if (page != null)
                            {
                                pageContent.Replace($"{match.Value}\n", page.Body);
                            }
                            else
                            {
                                StoreError(pageContent, match.Value, $"The include page was not found: [{navigation}]");
                            }
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    //Includes a processed wiki body into the calling page.
                    case "include": //(PageName)
                        {
                            var navigation = function.Parameters.Get<string>("pageName");

                            var page = WikiUtility.GetPageFromPathInfo(navigation);
                            if (page != null)
                            {
                                var wikify = new Wikifier(_sessionState, page, null, _omitMatches.ToArray(), _nestLevel + 1);

                                MergeUserVariables(wikify.UserVariables);
                                MergeSnippets(wikify.Snippets);

                                var identifier = StoreMatch(function, pageContent, match.Value, wikify.ProcessedBody);
                                pageContent.Replace($"{identifier}\n", $"{identifier}"); //Kill trailing newline.
                            }
                            else
                            {
                                StoreError(pageContent, match.Value, $"The include page was not found: [{navigation}]");
                            }
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "set":
                        {
                            var key = function.Parameters.Get<string>("key");
                            var value = function.Parameters.Get<string>("value");

                            if (UserVariables.ContainsKey(key))
                            {
                                UserVariables[key] = value;
                            }
                            else
                            {
                                UserVariables.Add(key, value);
                            }
                            var identifier = StoreMatch(function, pageContent, match.Value, string.Empty);
                            pageContent.Replace($"{identifier}\n", $"{identifier}"); //Kill trailing newline.
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "get":
                        {
                            var key = function.Parameters.Get<string>("key");

                            if (UserVariables.ContainsKey(key))
                            {
                                StoreMatch(function, pageContent, match.Value, UserVariables[key]);
                            }
                            else
                            {
                                throw new Exception($"The wiki variable {key} is not defined. It should be set with ##Set() before calling Get().");
                            }
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "color":
                        {
                            var color = function.Parameters.Get<string>("color");
                            var text = function.Parameters.Get<string>("text");
                            StoreMatch(function, pageContent, match.Value, $"<font color=\"{color}\">{text}</font>");
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    //Associates tags with a page. These are saved with the page and can also be displayed.
                    case "tag": //##tag(pipe|separated|list|of|tags)
                        {
                            var tags = function.Parameters.GetList<string>("pageTags");
                            Tags.AddRange(tags);
                            Tags = Tags.Distinct().ToList();
                            var identifier = StoreMatch(function, pageContent, match.Value, "");
                            pageContent.Replace($"{identifier}\n", $"{identifier}"); //Kill trailing newline.
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Displays an image that is attached to the page.
                    case "image": //##Image(Name, [optional:default=100]Scale, [optional:default=""]Alt-Text)
                        {
                            string imageName = function.Parameters.Get<string>("name");
                            string alt = function.Parameters.Get("alttext", imageName);
                            int scale = function.Parameters.Get<int>("scale");

                            bool explicitNamespace = imageName.Contains("::");
                            bool isPageForeignImage = false;

                            string navigation = _page.Navigation;
                            if (imageName.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
                            {
                                string image = $"<a href=\"{imageName}\" target=\"_blank\"><img src=\"{imageName}\" border=\"0\" alt=\"{alt}\" /></a>";
                                StoreMatch(function, pageContent, match.Value, image);
                            }
                            else if (imageName.Contains('/'))
                            {
                                //Allow loading attached images from other pages.
                                int slashIndex = imageName.IndexOf("/");
                                navigation = NamespaceNavigation.CleanAndValidate(imageName.Substring(0, slashIndex));
                                imageName = imageName.Substring(slashIndex + 1);
                                isPageForeignImage = true;
                            }

                            if (explicitNamespace == false && _page.Namespace != null)
                            {
                                if (PageFileRepository.GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation(navigation, NamespaceNavigation.CleanAndValidate(imageName), _revision) == null)
                                {
                                    //If the image does not exist, and no namespace was specified, but the page has a namespace - then default to the pages namespace.
                                    navigation = NamespaceNavigation.CleanAndValidate($"{_page.Namespace}::{imageName}");
                                }
                            }

                            if (_revision != null && isPageForeignImage == false)
                            {
                                //Check for isPageForeignImage because we don't version foreign page files.
                                string link = $"/Page/Image/{navigation}/{NamespaceNavigation.CleanAndValidate(imageName)}/{_revision}";
                                string image = $"<a href=\"{link}\" target=\"_blank\"><img src=\"{link}?Scale={scale}\" border=\"0\" alt=\"{alt}\" /></a>";
                                StoreMatch(function, pageContent, match.Value, image);
                            }
                            else
                            {
                                string link = $"/Page/Image/{navigation}/{NamespaceNavigation.CleanAndValidate(imageName)}";
                                string image = $"<a href=\"{link}\" target=\"_blank\"><img src=\"{link}?Scale={scale}\" border=\"0\" alt=\"{alt}\" /></a>";
                                StoreMatch(function, pageContent, match.Value, image);
                            }
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Displays an file download link
                    case "file": //##file(Name | Alt-Text | [optional display file size] true/false)
                        {
                            string fileName = function.Parameters.Get<string>("name");

                            bool explicitNamespace = fileName.Contains("::");
                            bool isPageForeignFile = false;

                            string navigation = _page.Navigation;
                            if (fileName.Contains('/'))
                            {
                                //Allow loading attached images from other pages.
                                int slashIndex = fileName.IndexOf("/");
                                navigation = NamespaceNavigation.CleanAndValidate(fileName.Substring(0, slashIndex));
                                fileName = fileName.Substring(slashIndex + 1);
                                isPageForeignFile = true;
                            }

                            if (explicitNamespace == false && _page.Namespace != null)
                            {
                                if (PageFileRepository.GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation(navigation, NamespaceNavigation.CleanAndValidate(fileName), _revision) == null)
                                {
                                    //If the image does not exist, and no namespace was specified, but the page has a namespace - then default to the pages namespace.
                                    navigation = NamespaceNavigation.CleanAndValidate($"{_page.Namespace}::{fileName}");
                                }
                            }

                            var attachment = PageFileRepository.GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation(navigation, NamespaceNavigation.CleanAndValidate(fileName), _revision);
                            if (attachment != null)
                            {
                                string alt = function.Parameters.Get("linkText", fileName);

                                if (function.Parameters.Get<bool>("showSize"))
                                {
                                    alt += $" ({attachment.FriendlySize})";
                                }

                                if (_revision != null && isPageForeignFile == false)
                                {
                                    //Check for isPageForeignImage because we don't version foreign page files.
                                    string link = $"/Page/Binary/{navigation}/{NamespaceNavigation.CleanAndValidate(fileName)}/{_revision}";
                                    string image = $"<a href=\"{link}\">{alt}</a>";
                                    StoreMatch(function, pageContent, match.Value, image);
                                }
                                else
                                {
                                    string link = $"/Page/Binary/{navigation}/{NamespaceNavigation.CleanAndValidate(fileName)}";
                                    string image = $"<a href=\"{link}\">{alt}</a>";
                                    StoreMatch(function, pageContent, match.Value, image);
                                }
                            }
                            else
                            {
                                StoreError(pageContent, match.Value, $"File not found [{fileName}]");
                            }
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Creates a list of pages that have been recently modified.
                    case "recentlymodified": //##RecentlyModified(TopCount)
                        {
                            string styleName = function.Parameters.Get<string>("styleName").ToLower();
                            var takeCount = function.Parameters.Get<int>("top");
                            var showNamespace = function.Parameters.Get<bool>("showNamespace");

                            var pages = PageRepository.GetTopRecentlyModifiedPagesInfo(takeCount)
                                .OrderByDescending(o => o.ModifiedDate).ThenBy(o => o.Title).ToList();

                            var html = new StringBuilder();

                            if (pages.Count() > 0)
                            {
                                html.Append("<ul>");
                                foreach (var page in pages)
                                {
                                    if (showNamespace)
                                    {
                                        html.Append($"<li><a href=\"/{page.Navigation}\">{page.Name}</a>");
                                    }
                                    else
                                    {
                                        html.Append($"<li><a href=\"/{page.Navigation}\">{page.Title}</a>");
                                    }

                                    if (styleName == "full")
                                    {
                                        if (page?.Description?.Length > 0)
                                        {
                                            html.Append(" - " + page.Description);
                                        }
                                    }
                                    html.Append("</li>");
                                }
                                html.Append("</ul>");
                            }

                            StoreMatch(function, pageContent, match.Value, html.ToString());
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Creates a glossary of pages in the specified namespace.
                    case "namespaceglossary":
                        {
                            string glossaryName = "glossary_" + new Random().Next(0, 1000000).ToString();
                            var namespaces = function.Parameters.GetList<string>("namespaces");

                            string styleName = function.Parameters.Get<string>("styleName").ToLower();
                            var topCount = function.Parameters.Get<int>("top");
                            var pages = PageRepository.GetPageInfoByNamespaces(namespaces).Take(topCount).OrderBy(o => o.Name).ToList();
                            var html = new StringBuilder();
                            var alphabet = pages.Select(p => p.Title.Substring(0, 1).ToUpper()).Distinct();
                            var showNamespace = function.Parameters.Get<bool>("showNamespace");

                            if (pages.Count() > 0)
                            {
                                html.Append("<center>");
                                foreach (var alpha in alphabet)
                                {
                                    html.Append("<a href=\"#" + glossaryName + "_" + alpha + "\">" + alpha + "</a>&nbsp;");
                                }
                                html.Append("</center>");

                                html.Append("<ul>");
                                foreach (var alpha in alphabet)
                                {
                                    html.Append("<li><a name=\"" + glossaryName + "_" + alpha + "\">" + alpha + "</a></li>");

                                    html.Append("<ul>");
                                    foreach (var page in pages.Where(p => p.Title.ToLower().StartsWith(alpha.ToLower())))
                                    {
                                        if (showNamespace)
                                        {
                                            html.Append($"<li><a href=\"/{page.Navigation}\">{page.Name}</a>");
                                        }
                                        else
                                        {
                                            html.Append($"<li><a href=\"/{page.Navigation}\">{page.Title}</a>");
                                        }

                                        if (styleName == "full")
                                        {
                                            if (page?.Description?.Length > 0)
                                            {
                                                html.Append(" - " + page.Description);
                                            }
                                        }
                                        html.Append("</li>");
                                    }
                                    html.Append("</ul>");
                                }

                                html.Append("</ul>");
                            }

                            StoreMatch(function, pageContent, match.Value, html.ToString());
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Creates a list of pages by searching the page tags.
                    case "namespacelist":
                        {
                            string styleName = function.Parameters.Get<string>("styleName").ToLower();
                            var topCount = function.Parameters.Get<int>("top");
                            var namespaces = function.Parameters.GetList<string>("namespaces");
                            var showNamespace = function.Parameters.Get<bool>("showNamespace");

                            var pages = PageRepository.GetPageInfoByNamespaces(namespaces).Take(topCount).OrderBy(o => o.Name).ToList();
                            var html = new StringBuilder();

                            if (pages.Count() > 0)
                            {
                                html.Append("<ul>");

                                foreach (var page in pages)
                                {
                                    if (showNamespace)
                                    {
                                        html.Append($"<li><a href=\"/{page.Navigation}\">{page.Name}</a>");
                                    }
                                    else
                                    {
                                        html.Append($"<li><a href=\"/{page.Navigation}\">{page.Title}</a>");
                                    }

                                    if (styleName == "full")
                                    {
                                        if (page?.Description?.Length > 0)
                                        {
                                            html.Append(" - " + page.Description);
                                        }
                                    }

                                    html.Append("</li>");
                                }

                                html.Append("</ul>");
                            }

                            StoreMatch(function, pageContent, match.Value, html.ToString());
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Creates a glossary of pages with the specified comma separated tags.
                    case "tagglossary":
                        {
                            string glossaryName = "glossary_" + new Random().Next(0, 1000000).ToString();
                            var tags = function.Parameters.GetList<string>("pageTags");

                            string styleName = function.Parameters.Get<string>("styleName").ToLower();
                            var topCount = function.Parameters.Get<int>("top");
                            var pages = PageRepository.GetPageInfoByTags(tags).Take(topCount).OrderBy(o => o.Name).ToList();
                            var html = new StringBuilder();
                            var alphabet = pages.Select(p => p.Title.Substring(0, 1).ToUpper()).Distinct();
                            var showNamespace = function.Parameters.Get<bool>("showNamespace");

                            if (pages.Count() > 0)
                            {
                                html.Append("<center>");
                                foreach (var alpha in alphabet)
                                {
                                    html.Append("<a href=\"#" + glossaryName + "_" + alpha + "\">" + alpha + "</a>&nbsp;");
                                }
                                html.Append("</center>");

                                html.Append("<ul>");
                                foreach (var alpha in alphabet)
                                {
                                    html.Append("<li><a name=\"" + glossaryName + "_" + alpha + "\">" + alpha + "</a></li>");

                                    html.Append("<ul>");
                                    foreach (var page in pages.Where(p => p.Title.ToLower().StartsWith(alpha.ToLower())))
                                    {
                                        if (showNamespace)
                                        {
                                            html.Append($"<li><a href=\"/{page.Navigation}\">{page.Name}</a>");
                                        }
                                        else
                                        {
                                            html.Append($"<li><a href=\"/{page.Navigation}\">{page.Title}</a>");
                                        }

                                        if (styleName == "full")
                                        {
                                            if (page?.Description?.Length > 0)
                                            {
                                                html.Append(" - " + page.Description);
                                            }
                                        }
                                        html.Append("</li>");
                                    }
                                    html.Append("</ul>");
                                }

                                html.Append("</ul>");
                            }

                            StoreMatch(function, pageContent, match.Value, html.ToString());
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Creates a glossary by searching page's body text for the specified comma separated list of words.
                    case "textglossary":
                        {
                            string glossaryName = "glossary_" + new Random().Next(0, 1000000).ToString();
                            var searchTokens = function.Parameters.Get<string>("searchPhrase").Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList();
                            var topCount = function.Parameters.Get<int>("top");
                            var showNamespace = function.Parameters.Get<bool>("showNamespace");

                            var pages = PageRepository.PageSearch(searchTokens).Take(topCount).OrderBy(o => o.Name).ToList();
                            var html = new StringBuilder();
                            var alphabet = pages.Select(p => p.Title.Substring(0, 1).ToUpper()).Distinct();
                            string styleName = function.Parameters.Get<string>("styleName").ToLower();

                            if (pages.Count() > 0)
                            {
                                html.Append("<center>");
                                foreach (var alpha in alphabet)
                                {
                                    html.Append("<a href=\"#" + glossaryName + "_" + alpha + "\">" + alpha + "</a>&nbsp;");
                                }
                                html.Append("</center>");

                                html.Append("<ul>");
                                foreach (var alpha in alphabet)
                                {
                                    html.Append("<li><a name=\"" + glossaryName + "_" + alpha + "\">" + alpha + "</a></li>");

                                    html.Append("<ul>");
                                    foreach (var page in pages.Where(p => p.Title.ToLower().StartsWith(alpha.ToLower())))
                                    {
                                        if (showNamespace)
                                        {
                                            html.Append($"<li><a href=\"/{page.Navigation}\">{page.Name}</a>");
                                        }
                                        else
                                        {
                                            html.Append($"<li><a href=\"/{page.Navigation}\">{page.Title}</a>");
                                        }

                                        if (styleName == "full")
                                        {
                                            if (page?.Description?.Length > 0)
                                            {
                                                html.Append(" - " + page.Description);
                                            }
                                        }
                                        html.Append("</li>");
                                    }
                                    html.Append("</ul>");
                                }

                                html.Append("</ul>");
                            }

                            StoreMatch(function, pageContent, match.Value, html.ToString());
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Creates a list of pages by searching the page body for the specified text.
                    case "searchlist":
                        {
                            string styleName = function.Parameters.Get<string>("styleName").ToLower();
                            string refTag = GenerateQueryToken();
                            int pageNumber = int.Parse(_queryString[refTag].ToString().DefaultWhenNullOrEmpty("1"));
                            var pageSize = function.Parameters.Get<int>("pageSize");
                            var pageSelector = function.Parameters.Get<bool>("pageSelector");
                            var allowFuzzyMatching = function.Parameters.Get<bool>("allowFuzzyMatching");
                            var searchTokens = function.Parameters.Get<string>("searchPhrase").Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList();
                            var showNamespace = function.Parameters.Get<bool>("showNamespace");

                            var pages = PageRepository.PageSearchPaged(searchTokens, pageNumber, pageSize, allowFuzzyMatching);
                            var html = new StringBuilder();

                            if (pages.Count() > 0)
                            {
                                html.Append("<ul>");

                                foreach (var page in pages)
                                {
                                    if (showNamespace)
                                    {
                                        html.Append($"<li><a href=\"/{page.Navigation}\">{page.Name}</a>");
                                    }
                                    else
                                    {
                                        html.Append($"<li><a href=\"/{page.Navigation}\">{page.Title}</a>");
                                    }

                                    if (styleName == "full")
                                    {
                                        if (page?.Description?.Length > 0)
                                        {
                                            html.Append(" - " + page.Description);
                                        }
                                    }

                                    html.Append("</li>");
                                }

                                html.Append("</ul>");
                            }

                            if (pageSelector && (pageNumber > 1 || (pages.Count > 0 && pages.First().PaginationPageCount > 1)))
                            {
                                html.Append(PageSelectorGenerator.Generate(refTag, _queryString, pages.FirstOrDefault()?.PaginationPageCount ?? 1));
                            }

                            StoreMatch(function, pageContent, match.Value, html.ToString());
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Creates a list of pages by searching the page tags.
                    case "taglist":
                        {
                            string styleName = function.Parameters.Get<string>("styleName").ToLower();
                            var topCount = function.Parameters.Get<int>("top");
                            var tags = function.Parameters.GetList<string>("pageTags");
                            var showNamespace = function.Parameters.Get<bool>("showNamespace");

                            var pages = PageRepository.GetPageInfoByTags(tags).Take(topCount).OrderBy(o => o.Name).ToList();
                            var html = new StringBuilder();

                            if (pages.Count() > 0)
                            {
                                html.Append("<ul>");

                                foreach (var page in pages)
                                {
                                    if (showNamespace)
                                    {
                                        html.Append($"<li><a href=\"/{page.Navigation}\">{page.Name}</a>");
                                    }
                                    else
                                    {
                                        html.Append($"<li><a href=\"/{page.Navigation}\">{page.Title}</a>");
                                    }

                                    if (styleName == "full")
                                    {
                                        if (page?.Description?.Length > 0)
                                        {
                                            html.Append(" - " + page.Description);
                                        }
                                    }

                                    html.Append("</li>");
                                }

                                html.Append("</ul>");
                            }

                            StoreMatch(function, pageContent, match.Value, html.ToString());
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    //Displays a list of other related pages based on tags.
                    case "similar": //##Similar()
                        {
                            string refTag = GenerateQueryToken();

                            var similarity = function.Parameters.Get<int>("similarity");
                            int pageNumber = int.Parse(_queryString[refTag].ToString().DefaultWhenNullOrEmpty("1"));
                            var pageSize = function.Parameters.Get<int>("pageSize");
                            var pageSelector = function.Parameters.Get<bool>("pageSelector");
                            string styleName = function.Parameters.Get<string>("styleName").ToLower();
                            var html = new StringBuilder();

                            var pages = PageRepository.GetSimilarPagesPaged(_page.Id, similarity, pageNumber, pageSize);

                            if (styleName == "list")
                            {
                                html.Append("<ul>");
                                foreach (var page in pages)
                                {
                                    html.Append($"<li><a href=\"/{page.Navigation}\">{page.Title}</a>");
                                }
                                html.Append("</ul>");
                            }
                            else if (styleName == "flat")
                            {
                                foreach (var page in pages)
                                {
                                    if (html.Length > 0) html.Append(" | ");
                                    html.Append($"<a href=\"/{page.Navigation}\">{page.Title}</a>");
                                }
                            }
                            else if (styleName == "full")
                            {
                                html.Append("<ul>");
                                foreach (var page in pages)
                                {
                                    html.Append($"<li><a href=\"/{page.Navigation}\">{page.Title}</a> - {page.Description}");
                                }
                                html.Append("</ul>");
                            }

                            if (pageSelector && pages.Count > 0 && pages.First().PaginationPageCount > 1)
                            {
                                html.Append(PageSelectorGenerator.Generate(refTag, _queryString, pages.First().PaginationPageCount));
                            }

                            StoreMatch(function, pageContent, match.Value, html.ToString());
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Displays a list of other related pages based incoming links.
                    case "related": //##related
                        {
                            string refTag = GenerateQueryToken();

                            int pageNumber = int.Parse(_queryString[refTag].ToString().DefaultWhenNullOrEmpty("1"));
                            var pageSize = function.Parameters.Get<int>("pageSize");
                            var pageSelector = function.Parameters.Get<bool>("pageSelector");
                            string styleName = function.Parameters.Get<string>("styleName").ToLower();
                            var html = new StringBuilder();

                            var pages = PageRepository.GetRelatedPagesPaged(_page.Id, pageNumber, pageSize);

                            if (styleName == "list")
                            {
                                html.Append("<ul>");
                                foreach (var page in pages)
                                {
                                    html.Append($"<li><a href=\"/{page.Navigation}\">{page.Title}</a>");
                                }
                                html.Append("</ul>");
                            }
                            else if (styleName == "flat")
                            {
                                foreach (var page in pages)
                                {
                                    if (html.Length > 0) html.Append(" | ");
                                    html.Append($"<a href=\"/{page.Navigation}\">{page.Title}</a>");
                                }
                            }
                            else if (styleName == "full")
                            {
                                html.Append("<ul>");
                                foreach (var page in pages)
                                {
                                    html.Append($"<li><a href=\"/{page.Navigation}\">{page.Title}</a> - {page.Description}");
                                }
                                html.Append("</ul>");
                            }

                            if (pageSelector && pages.Count > 0 && pages.First().PaginationPageCount > 1)
                            {
                                html.Append(PageSelectorGenerator.Generate(refTag, _queryString, pages.First().PaginationPageCount));
                            }

                            StoreMatch(function, pageContent, match.Value, html.ToString());
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Displays the date and time that the current page was last modified.
                    case "lastmodified":
                        {
                            if (_sessionState == null)
                            {
                                StoreError(pageContent, match.Value, $"Localization is not supported without SessionState.");
                                continue;
                            }

                            DateTime lastModified = DateTime.MinValue;
                            lastModified = _page.ModifiedDate;
                            if (lastModified != DateTime.MinValue)
                            {
                                var localized = _sessionState.LocalizeDateTime(lastModified);
                                StoreMatch(function, pageContent, match.Value, $"{localized.ToShortDateString()} {localized.ToShortTimeString()}");
                            }
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Displays the date and time that the current page was created.
                    case "created":
                        {
                            if (_sessionState == null)
                            {
                                StoreError(pageContent, match.Value, $"Localization is not supported without SessionState.");
                                continue;
                            }

                            DateTime createdDate = DateTime.MinValue;
                            createdDate = _page.CreatedDate;
                            if (createdDate != DateTime.MinValue)
                            {
                                var localized = _sessionState.LocalizeDateTime(createdDate);
                                StoreMatch(function, pageContent, match.Value, $"{localized.ToShortDateString()} {localized.ToShortTimeString()}");
                            }
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Displays the version of the wiki.
                    case "appversion":
                        {
                            var version = string.Join('.', (Assembly.GetExecutingAssembly()
                                .GetName().Version?.ToString() ?? "0.0.0.0").Split('.').Take(3)); //Major.Minor.Patch

                            StoreMatch(function, pageContent, match.Value, version);
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Displays the title of the current page.
                    case "name":
                        {
                            StoreMatch(function, pageContent, match.Value, _page.Title);
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Displays the title of the current page in title form.
                    case "title":
                        {
                            StoreMatch(function, pageContent, match.Value, $"<h1>{_page.Title}</h1>");
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Displays the namespace of the current page.
                    case "namespace":
                        {
                            StoreMatch(function, pageContent, match.Value, _page.Namespace ?? string.Empty);
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Displays the namespace of the current page.
                    case "snippet":
                        {
                            string name = function.Parameters.Get<string>("name");

                            if (Snippets.ContainsKey(name))
                            {
                                StoreMatch(function, pageContent, match.Value, Snippets[name]);
                            }
                            else
                            {
                                StoreMatch(function, pageContent, match.Value, string.Empty);
                            }
                        }
                        break;


                    //------------------------------------------------------------------------------------------------------------------------------
                    //Inserts empty lines into the page.
                    case "br":
                    case "nl":
                    case "newline": //##NewLine([optional:default=1]count)
                        {
                            int count = function.Parameters.Get<int>("Count");
                            for (int i = 0; i < count; i++)
                            {
                                StoreMatch(function, pageContent, match.Value, $"<br />");
                            }
                        }
                        break;

                    //Inserts a horizontal rule
                    case "hr":
                        {
                            int size = function.Parameters.Get<int>("height");
                            StoreMatch(function, pageContent, match.Value, $"<hr class=\"mt-{size} mb-{size}\">");
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Displays the navigation text for the current page.
                    case "navigation":
                        {
                            string navigation = _page.Navigation;
                            if (navigation != string.Empty)
                            {
                                StoreMatch(function, pageContent, match.Value, navigation);
                            }
                        }
                        break;
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

            foreach (var match in orderedMatches)
            {
                FunctionCall function;

                try
                {
                    function = FunctionParser.ParseFunctionCall(match.Value, out int matchEndIndex);
                }
                catch (Exception ex)
                {
                    StoreError(pageContent, match.Value, ex.Message);
                    continue;
                }

                switch (function.Name.ToLower())
                {
                    //------------------------------------------------------------------------------------------------------------------------------
                    //Displays a tag link list.
                    case "tags": //##tags
                        {
                            string styleName = function.Parameters.Get<string>("styleName").ToLower();
                            var html = new StringBuilder();

                            if (styleName == "list")
                            {
                                html.Append("<ul>");
                                foreach (var tag in Tags)
                                {
                                    html.Append($"<li><a href=\"/Tag/Browse/{tag}\">{tag}</a>");
                                }
                                html.Append("</ul>");
                            }
                            else if (styleName == "flat")
                            {
                                foreach (var tag in Tags)
                                {
                                    if (html.Length > 0) html.Append(" | ");
                                    html.Append($"<a href=\"/Tag/Browse/{tag}\">{tag}</a>");
                                }
                            }

                            StoreMatch(function, pageContent, match.Value, html.ToString());
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    case "tagcloud":
                        {
                            var top = function.Parameters.Get<int>("Top");
                            string seedTag = function.Parameters.Get<string>("pageTag");

                            string cloudHtml = TagCloud.Build(seedTag, top);
                            StoreMatch(function, pageContent, match.Value, cloudHtml);
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    case "searchcloud":
                        {
                            var top = function.Parameters.Get<int>("Top");
                            var tokens = function.Parameters.Get<string>("searchPhrase").Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList();

                            string cloudHtml = SearchCloud.Build(tokens, top);
                            StoreMatch(function, pageContent, match.Value, cloudHtml);
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Diplays a table of contents for the page based on the header tags.
                    case "toc":
                        {
                            bool alphabetized = function.Parameters.Get<bool>("alphabetized");

                            var html = new StringBuilder();

                            var tags = (from t in _tocTags
                                        orderby t.StartingPosition
                                        select t).ToList();

                            var unordered = new List<TOCTag>();
                            var ordered = new List<TOCTag>();

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

                            StoreMatch(function, pageContent, match.Value, html.ToString());
                        }
                        break;
                }
            }
        }

        private void StoreCriticalError(Exception ex)
        {
            ExceptionRepository.InsertException(ex, $"Page: {_page.Navigation}, Error: {ex.Message}");

            ErrorCount++;
            ProcessedBody = WikiUtility.WarningCard("Wiki Parser Exception", ex.Message);
        }

        private string StoreError(WikiString pageContent, string match, string value)
        {
            ExceptionRepository.InsertException($"Page: {_page.Navigation}, Error: {value}");

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

        private string StoreFirstMatch(FunctionCall function, WikiString pageContent, string match, string value, bool allowNestedDecode = true)
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
        private string GenerateQueryToken()
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

        private void MergeUserVariables(Dictionary<string, string> items)
        {
            foreach (var item in items)
            {
                UserVariables[item.Key] = item.Value;
            }
        }

        private void MergeSnippets(Dictionary<string, string> items)
        {
            foreach (var item in items)
            {
                Snippets[item.Key] = item.Value;
            }
        }
    }
}
