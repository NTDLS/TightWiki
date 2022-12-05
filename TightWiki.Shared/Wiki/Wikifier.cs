using DuoVia.FuzzyStrings;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using TightWiki.Shared.Library;
using TightWiki.Shared.Models;
using TightWiki.Shared.Models.Data;
using TightWiki.Shared.Repository;
using TightWiki.Shared.Wiki.Function;
using static TightWiki.Shared.Library.Constants;
using static TightWiki.Shared.Wiki.Constants;

namespace TightWiki.Shared.Wiki
{
    public class Wikifier
    {
        public int ErrorCount { get; private set; }
        public int MatchCount { get; private set; }
        public TimeSpan ProcessingTime { get; private set; }

        private const string SoftBreak = "<!--SoftBreak-->"; //These will remain as \r\n in the final HTML.
        private const string HardBreak = "<!--HardBreak-->"; //These will remain as <br /> in the final HTML.

        private readonly Dictionary<string, string> _userVariables = new Dictionary<string, string>();
        public List<NameNav> OutgoingLinks { get; set; } = new List<NameNav>();
        public List<string> ProcessingInstructions { get; private set; } = new List<string>();
        public string ProcessedBody { get; private set; }
        public List<string> Tags { get; private set; } = new List<string>();
        public Dictionary<string, MatchSet> Matches { get; private set; } = new Dictionary<string, MatchSet>();

        private string _queryTokenState = Security.MachineKey;
        private int _matchesPerIteration = 0;
        private readonly string _tocName = "TOC_" + (new Random()).Next(0, 1000000).ToString();
        private readonly List<TOCTag> _tocTags = new List<TOCTag>();
        private readonly Page _page;
        private readonly int? _revision;
        readonly IQueryCollection _queryString;
        private readonly StateContext _context;
        private readonly int _nestLevel;

        /// <summary>
        /// When matches are omitted, the entire match will be removed from the resulting wiki text.
        /// </summary>
        private List<WikiMatchType> _omitMatches = new List<WikiMatchType>();

        public Wikifier(StateContext context, Page page, int? revision = null, IQueryCollection queryString = null, WikiMatchType[] omitMatches = null, int nestLevel = 0)
        {
            DateTime startTime = DateTime.UtcNow;

            _nestLevel = nestLevel;
            _queryString = queryString;
            _page = page;
            _revision = revision;
            Matches = new Dictionary<string, MatchSet>();
            _context = context;

            if (omitMatches != null)
            {
                _omitMatches.AddRange(omitMatches);
            }

            try
            {
                Transform();
            }
            catch (Exception ex)
            {
                StoreCriticalError(ex.Message);
            }

            ProcessingTime = DateTime.UtcNow - startTime;
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
            var pageContent = new StringBuilder(_page.Body);

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
                        pageContent.Replace(v.Key, string.Empty);
                    }
                    else
                    {
                        pageContent.Replace(v.Key, v.Value.Content);
                    }
                }
            } while (length != pageContent.Length);

            if (_revision != null)
            {
                var revision = PageRepository.GetPageRevisionInfoById(_page.Id, _revision);
                var pageInfo = PageRepository.GetPageInfoById(_page.Id);

                var html = new StringBuilder();

                html.Append("<div class=\"card bg-warning mb-3\">");
                html.Append($"<div class=\"card-header\">Viewing a historical revision</div>");
                html.Append("<div class=\"card-body\">");
                html.Append($"<p class=\"card-text\">You are viewing revision {_revision:0} of \"{_page.Name}\" modified by \"{revision.ModifiedByUserName}\" on {_context.LocalizeDateTime(revision.ModifiedDate)}. <br />");
                html.Append($"<a href=\"/{_page.Navigation}\">View the latest revision {pageInfo.Revision:0}.</a>");
                html.Append("</p>");
                html.Append("</div>");
                html.Append("</div>");
                pageContent.Insert(0, html);
            }

            pageContent.Replace(SoftBreak, "\r\n");
            pageContent.Replace(HardBreak, "<br />");

            ProcessedBody = pageContent.ToString();
        }

        public int TransformAll(StringBuilder pageContent)
        {
            _matchesPerIteration = 0;

            TransformComments(pageContent);
            TransformBlocks(pageContent);
            TransformVariables(pageContent);
            TransformLinks(pageContent);
            TransformMarkup(pageContent);
            TransformSectionHeadings(pageContent);
            TransformFunctions(pageContent);
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
        private void TransformMarkup(StringBuilder pageContent)
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

            var regEx = new StringBuilder();
            regEx.Append(@"(\^\^\^\^\^\^\^.*?\n)");
            regEx.Append(@"|");
            regEx.Append(@"(\^\^\^\^\^\^.*?\n)");
            regEx.Append(@"|");
            regEx.Append(@"(\^\^\^\^\^.*?\n)");
            regEx.Append(@"|");
            regEx.Append(@"(\^\^\^\^.*?\n)");
            regEx.Append(@"|");
            regEx.Append(@"(\^\^\^.*?\n)");
            regEx.Append(@"|");
            regEx.Append(@"(\^\^.*?\n)");

            var rgx = new Regex(regEx.ToString(), RegexOptions.IgnoreCase);
            var matches = WikiUtility.OrderMatchesByLengthDescending(rgx.Matches(pageContent.ToString()));

            foreach (var match in matches)
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
                    string tag = _tocName + "_" + _tocTags.Count().ToString();
                    string value = match.Value.Substring(headingMarkers, match.Value.Length - headingMarkers).Trim();

                    int fontSize = 1 + headingMarkers;
                    if (fontSize < 1) fontSize = 1;

                    string link = "<font size=\"" + fontSize + "\">" + value + "</span></font>\r\n";
                    StoreMatch(WikiMatchType.Formatting, pageContent, match.Value, link);
                }
            }
        }

        /// <summary>
        /// Transform inline and multi-line literal blocks. These are blocks where the content will not be wikified and contain code that is encoded to display verbatim on the page.
        /// </summary>
        /// <param name="pageContent"></param>
        private void TransformLiterals(StringBuilder pageContent)
        {
            //TODO: May need to do the same thing we did with TransformBlocks() to match all these if they need to be nested.

            //Transform literal strings, even encodes HTML so that it displays verbatim.
            Regex rgx = new Regex(@"\#\{([\S\s]*?)\}\#", RegexOptions.IgnoreCase);
            var matches = WikiUtility.OrderMatchesByLengthDescending(rgx.Matches(pageContent.ToString()));
            foreach (var match in matches)
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
        private void TransformBlocks(StringBuilder pageContent)
        {
            var content = pageContent.ToString();

            string rawBlock = string.Empty;

            while (true)
            {
                int startPos = content.LastIndexOf("{{{");
                if (startPos < 0)
                {
                    break;
                }
                int endPos = content.IndexOf("}}}", startPos);

                if (endPos < 0 || endPos < startPos)
                {
                    var exception = new StringBuilder();
                    exception.AppendLine($"<strong>A parsing error occured after position {startPos}:<br /></strong> Unable to locate closing tag.<br /><br />");
                    if (rawBlock?.Length > 0)
                    {
                        exception.AppendLine($"<strong>The last successfully parsed block was:</strong><br /> {rawBlock}");
                    }
                    exception.AppendLine($"<strong>The problem occured after:</strong><br /> {pageContent.ToString().Substring(startPos)}<br /><br />");
                    exception.AppendLine($"<strong>The content the parser was working on is:</strong><br /> {pageContent}<br /><br />");

                    throw new Exception(exception.ToString());
                }

                rawBlock = content.Substring(startPos, (endPos - startPos) + 3);
                var transformBlock = new StringBuilder(rawBlock);
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
        private void TransformBlock(StringBuilder pageContent, bool firstBlocks)
        {
            Regex rgx = new Regex(@"{{{([\S\s]*)}}}", RegexOptions.IgnoreCase);
            var matches = WikiUtility.OrderMatchesByLengthDescending(rgx.Matches(pageContent.ToString()));
            foreach (var match in matches)
            {
                int paramEndIndex = -1;

                FunctionCallInstance function;

                //We are going to mock up a function call:
                var originalMatchValue = match.Value;
                match.Value = "##" + match.Value.Trim(new char[] { ' ', '\t', '{', '}' });

                try
                {
                    function = FunctionParser.ParseFunctionCallInfo(match, out paramEndIndex);
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
                        case "alert":
                            {
                                string titleText = function.Parameters.Get<string>("titleText");
                                string style = function.Parameters.Get<string>("styleName").ToLower();
                                style = (style == "default" ? "" : $"alert-{style}");

                                if (!string.IsNullOrEmpty(titleText)) scopeBody = $"<h1>{titleText}</h1>{scopeBody}";
                                html.Append($"<div class=\"alert {style}\">{scopeBody}.</div>");
                            }
                            break;

                        //------------------------------------------------------------------------------------------------------------------------------
                        case "jumbotron":
                            {
                                string titleText = function.Parameters.Get<string>("titleText", "");
                                if (!string.IsNullOrEmpty(titleText)) scopeBody = $"<h1>{titleText}</h1>{scopeBody}";
                                html.Append($"<div class=\"jumbotron\">{scopeBody}</div>");
                            }
                            break;
                        //------------------------------------------------------------------------------------------------------------------------------
                        case "foreground":
                            {
                                var style = WikiUtility.GetForegroundStyle(function.Parameters.Get<string>("styleName", "default")).Swap();
                                html.Append($"<p class=\"{style.ForegroundStyle} {style.BackgroundStyle}\">{scopeBody}</p>");
                            }
                            break;
                        //------------------------------------------------------------------------------------------------------------------------------
                        case "background":
                            {
                                var style = WikiUtility.GetBackgroundStyle(function.Parameters.Get<string>("styleName", "default"));
                                html.Append($"<div class=\"p-3 mb-2 {style.ForegroundStyle} {style.BackgroundStyle}\">{scopeBody}</div>");
                            }
                            break;
                        //------------------------------------------------------------------------------------------------------------------------------
                        case "collapse":
                            {
                                string linkText = function.Parameters.Get<string>("linktext");
                                string uid = "A" + Guid.NewGuid().ToString().Replace("-", "");
                                html.Append($"<a data-toggle=\"collapse\" href=\"#{uid}\" role=\"button\" aria-expanded=\"false\" aria-controls=\"{uid}\">{linkText}</a>");
                                html.Append($"<div class=\"collapse\" id=\"{uid}\">");
                                html.Append($"<div class=\"card card-body\"><p class=\"card-text\">{scopeBody}</p></div></div>");
                            }
                            break;
                        //------------------------------------------------------------------------------------------------------------------------------
                        case "callout":
                            {
                                string titleText = function.Parameters.Get<string>("titleText");
                                string style = function.Parameters.Get<string>("styleName").ToLower();
                                style = (style == "default" ? "" : style);

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
                                var style = WikiUtility.GetBackgroundStyle(function.Parameters.Get<string>("styleName", "default"));

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
        void TransformSectionHeadings(StringBuilder pageContent)
        {
            var regEx = new StringBuilder();
            regEx.Append(@"(\=\=\=\=\=\=\=.*?\n)");
            regEx.Append(@"|");
            regEx.Append(@"(\=\=\=\=\=\=.*?\n)");
            regEx.Append(@"|");
            regEx.Append(@"(\=\=\=\=\=.*?\n)");
            regEx.Append(@"|");
            regEx.Append(@"(\=\=\=\=.*?\n)");
            regEx.Append(@"|");
            regEx.Append(@"(\=\=\=.*?\n)");
            regEx.Append(@"|");
            regEx.Append(@"(\=\=.*?\n)");

            Regex rgx = new Regex(regEx.ToString(), RegexOptions.IgnoreCase);
            var matches = WikiUtility.OrderMatchesByLengthDescending(rgx.Matches(pageContent.ToString()));

            foreach (var match in matches)
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

        private string GetLinkImage(string linkText)
        {
            string compareString = linkText.ToLower().RemoveWhitespace();

            //Internal page attached image:
            if (compareString.StartsWith("img="))
            {
                if (linkText.Contains("/"))
                {
                    linkText = linkText.Substring(linkText.IndexOf("=") + 1);
                    string scale = "100";

                    //Allow loading attacehd images from other pages.
                    int slashIndex = linkText.IndexOf("/");
                    string navigation = WikiUtility.CleanPartialURI(linkText.Substring(0, slashIndex));
                    linkText = linkText.Substring(slashIndex + 1);

                    int scaleIndex = linkText.IndexOf("|");
                    if (scaleIndex > 0)
                    {
                        scale = linkText.Substring(scaleIndex + 1);
                        linkText = linkText.Substring(0, scaleIndex);
                    }

                    if (_revision != null)
                    {
                        string attachementLink = $"/File/Image/{navigation}/{WikiUtility.CleanPartialURI(linkText)}/r/{_revision}";
                        linkText = $"<img src=\"{attachementLink}?Scale={scale}\" border=\"0\" />";
                    }
                    else
                    {
                        string attachementLink = $"/File/Image/{navigation}/{WikiUtility.CleanPartialURI(linkText)}";
                        linkText = $"<img src=\"{attachementLink}?Scale={scale}\" border=\"0\" />";
                    }
                }
                else
                {
                    linkText = linkText.Substring(linkText.IndexOf("=") + 1);
                    string scale = "100";

                    int scaleIndex = linkText.IndexOf("|");
                    if (scaleIndex > 0)
                    {
                        scale = linkText.Substring(scaleIndex + 1);
                        linkText = linkText.Substring(0, scaleIndex);
                    }

                    if (_revision != null)
                    {
                        string attachementLink = $"/File/Image/{_page.Navigation}/{WikiUtility.CleanPartialURI(linkText)}/r/{_revision}";
                        linkText = $"<img src=\"{attachementLink}?Scale={scale}\" border=\"0\" />";
                    }
                    else
                    {
                        string attachementLink = $"/File/Image/{_page.Navigation}/{WikiUtility.CleanPartialURI(linkText)}";
                        linkText = $"<img src=\"{attachementLink}?Scale={scale}\" border=\"0\" />";
                    }
                }
            }
            //External site image:
            else if (compareString.StartsWith("src="))
            {
                linkText = linkText.Substring(linkText.IndexOf("=") + 1);
                linkText = $"<img src=\"{linkText}\" border=\"0\" />";
            }

            return linkText;
        }

        private void TransformComments(StringBuilder pageContent)
        {
            Regex rgx = new Regex(@"\;\;.*", RegexOptions.IgnoreCase);
            var matches = WikiUtility.OrderMatchesByLengthDescending(rgx.Matches(pageContent.ToString()));
            foreach (var match in matches)
            {
                string key = match.Value.Trim(new char[] { '{', '}', ' ', '\t', '$' });

                var identifier = StoreMatch(WikiMatchType.Instruction, pageContent, match.Value, "");
                pageContent.Replace($"{identifier}\n", $"{identifier}"); //Kill trailing newline.
            }
        }

        /// <summary>
        /// Transform variables.
        /// </summary>
        /// <param name="pageContent"></param>
        private void TransformVariables(StringBuilder pageContent)
        {
            Regex rgx = new Regex(@"(\$\{.+?\})", RegexOptions.IgnoreCase);
            var matches = WikiUtility.OrderMatchesByLengthDescending(rgx.Matches(pageContent.ToString()));
            foreach (var match in matches)
            {
                string key = match.Value.Trim(new char[] { '{', '}', ' ', '\t', '$' });
                if (key.Contains("="))
                {
                    var sections = key.Split('=');
                    key = sections[0].Trim();
                    var value = sections[1].Trim();

                    if (_userVariables.ContainsKey(key))
                    {
                        _userVariables[key] = value;
                    }
                    else
                    {
                        _userVariables.Add(key, value);
                    }

                    var identifier = StoreMatch(WikiMatchType.Instruction, pageContent, match.Value, "");
                    pageContent.Replace($"{identifier}\n", $"{identifier}"); //Kill trailing newline.
                }
                else
                {
                    if (_userVariables.ContainsKey(key))
                    {
                        var identifier = StoreMatch(WikiMatchType.Variable, pageContent, match.Value, _userVariables[key]);
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
        private void TransformLinks(StringBuilder pageContent)
        {
            //Parse external explicit links. eg. [[http://test.net]].
            Regex rgx = new Regex(@"(\[\[http\:\/\/.+?\]\])", RegexOptions.IgnoreCase);
            var matches = WikiUtility.OrderMatchesByLengthDescending(rgx.Matches(pageContent.ToString()));
            foreach (var match in matches)
            {
                string keyword = match.Value.Substring(2, match.Value.Length - 4).Trim();
                int pipeIndex = keyword.IndexOf("|");
                if (pipeIndex > 0)
                {
                    string linkText = keyword.Substring(pipeIndex + 1).Trim();
                    if (linkText.StartsWith("src=", StringComparison.CurrentCultureIgnoreCase))
                    {
                        linkText = $"<img {linkText} border =\"0\" > ";
                    }

                    keyword = keyword.Substring(0, pipeIndex).Trim();

                    StoreMatch(WikiMatchType.Link, pageContent, match.Value, "<a href=\"" + keyword + "\">" + linkText + "</a>");
                }
                else
                {
                    StoreMatch(WikiMatchType.Link, pageContent, match.Value, "<a href=\"" + keyword + "\">" + keyword + "</a>");
                }
            }

            //Parse internal dynamic links. eg [[AboutUs|About Us]].
            rgx = new Regex(@"(\[\[.+?\]\])", RegexOptions.IgnoreCase);
            matches = WikiUtility.OrderMatchesByLengthDescending(rgx.Matches(pageContent.ToString()));
            foreach (var match in matches)
            {
                string keyword = match.Value.Substring(2, match.Value.Length - 4);
                string explicitLinkText = "";
                string linkText;

                int pipeIndex = keyword.IndexOf("|");
                if (pipeIndex > 0)
                {
                    explicitLinkText = keyword.Substring(pipeIndex + 1).Trim();
                    keyword = keyword.Substring(0, pipeIndex).Trim();
                }


                string pageName = keyword;
                string pageNavigation = WikiUtility.CleanPartialURI(pageName);
                var page = PageRepository.GetPageRevisionByNavigation(pageNavigation);

                OutgoingLinks.Add(new NameNav(pageName, pageNavigation));

                if (page != null)
                {
                    if (explicitLinkText.Length == 0)
                    {
                        linkText = page.Name;
                    }
                    else
                    {
                        linkText = GetLinkImage(explicitLinkText);
                    }

                    StoreMatch(WikiMatchType.Link, pageContent, match.Value, "<a href=\"" + WikiUtility.CleanFullURI($"/{pageNavigation}") + $"\">{linkText}</a>");
                }
                else if (_context?.CanCreate == true)
                {
                    if (explicitLinkText.Length == 0)
                    {
                        linkText = pageName;
                    }
                    else
                    {
                        linkText = explicitLinkText;
                    }

                    linkText += "<font color=\"#cc0000\" size=\"2\">?</font>";
                    StoreMatch(WikiMatchType.Link, pageContent, match.Value, "<a href=\"" + WikiUtility.CleanFullURI($"/Page/Edit/{pageNavigation}/") + $"?Name={pageName}\">{linkText}</a>");
                }
                else
                {
                    if (explicitLinkText.Length == 0)
                    {
                        linkText = pageName;
                    }
                    else
                    {
                        linkText = explicitLinkText;
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
        private void TransformProcessingInstructions(StringBuilder pageContent)
        {
            Regex rgx = new Regex(@"(\@\@[\w-]+\(\))|(\@\@[\w-]+\(.*?\))|(\@\@[\w-]+)", RegexOptions.IgnoreCase);
            var matches = WikiUtility.OrderMatchesByLengthDescending(rgx.Matches(pageContent.ToString()));
            foreach (var match in matches)
            {
                FunctionCallInstance function;

                try
                {
                    function = FunctionParser.ParseFunctionCallInfo(match, out int matchEndIndex);
                }
                catch (Exception ex)
                {
                    StoreError(pageContent, match.Value, ex.Message);
                    continue;
                }

                switch (function.Name.ToLower())
                {
                    //We check _nestLevel here because we dont want to include the processing instructions on any parent pages that are injecting this one.

                    //------------------------------------------------------------------------------------------------------------------------------
                    case "deprecate":
                        {
                            if (_nestLevel == 0)
                            {
                                ProcessingInstructions.Add(WikiInstruction.Deprecate);
                                pageContent.Insert(0, "<div class=\"alert alert-danger\">This page has been deprecated and will eventualy be deleted.</div>");
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
                                pageContent.Insert(0, "<div class=\"alert alert-info\">This page is a template and will not appear in indexes or glossaries.</div>");
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
                                pageContent.Insert(0, "<div class=\"alert alert-info\">This page is an include and will not appear in indexes or glossaries.</div>");
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
        private void TransformFunctions(StringBuilder pageContent)
        {
            //Remove the last "(\#\#[\w-]+)" if you start to have matching problems:
            Regex rgx = new Regex(@"(\#\#[\w-]+\(\))|(##|{{|@@)([a-zA-Z_\s{][a-zA-Z0-9_\s{]*)\(((?<BR>\()|(?<-BR>\))|[^()]*)+\)|(\#\#[\w-]+)", RegexOptions.IgnoreCase);
            var matches = WikiUtility.OrderMatchesByLengthDescending(rgx.Matches(pageContent.ToString()));

            foreach (var match in matches)
            {
                FunctionCallInstance function;

                try
                {
                    function = FunctionParser.ParseFunctionCallInfo(match, out int matchEndIndex);
                }
                catch (Exception ex)
                {
                    StoreError(pageContent, match.Value, ex.Message);
                    continue;
                }

                switch (function.Name.ToLower())
                {
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "attachments":
                        {
                            string refTag = GenerateQueryToken();

                            int pageNumber = int.Parse(_queryString[refTag].ToString().IsNullOrEmpty("1"));

                            var navigation = WikiUtility.CleanPartialURI(function.Parameters.Get<string>("pageName", _page.Navigation));
                            string styleName = function.Parameters.Get<String>("styleName").ToLower();
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
                                        html.Append($"<li><a href=\"/File/Binary/{_page.Navigation}/{file.FileNavigation}/r/{_revision}\">{file.Name}</a>");
                                    }
                                    else
                                    {
                                        html.Append($"<li><a href=\"/File/Binary/{_page.Navigation}/{file.FileNavigation}\">{file.Name} </a>");
                                    }

                                    if (styleName == "full")
                                    {
                                        html.Append($" - ({file.FriendlySize})");
                                    }

                                    html.Append("</li>");
                                }
                                html.Append("</ul>");

                                if (pageSelector && attachments.Count > 0 && attachments.First().PaginationCount > 1)
                                {
                                    html.Append(WikiUtility.GetPageSelector(refTag, attachments.First().PaginationCount, pageNumber, _queryString));
                                }
                            }

                            StoreMatch(function, pageContent, match.Value, html.ToString());
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "history":
                        {
                            string refTag = GenerateQueryToken();

                            int pageNumber = int.Parse(_queryString[refTag].ToString().IsNullOrEmpty("1"));

                            var navigation = WikiUtility.CleanPartialURI(function.Parameters.Get<string>("pageName", _page.Navigation));
                            string styleName = function.Parameters.Get<String>("styleName").ToLower();
                            var pageSize = function.Parameters.Get<int>("pageSize");
                            var pageSelector = function.Parameters.Get<bool>("pageSelector");
                            var history = PageRepository.GetPageRevisionHistoryInfoByNavigationPaged(navigation, pageNumber, pageSize);
                            var html = new StringBuilder();

                            if (history.Count() > 0)
                            {
                                html.Append("<ul>");
                                foreach (var item in history)
                                {
                                    html.Append($"<li><a href=\"/{item.Navigation}/r/{item.Revision}\">{item.Revision} by {item.ModifiedByUserName} on {_context.LocalizeDateTime(item.ModifiedDate)}</a>");

                                    if (styleName == "full")
                                    {
                                        var thisRev = PageRepository.GetPageRevisionByNavigation(_page.Navigation, item.Revision);
                                        var prevRev = PageRepository.GetPageRevisionByNavigation(_page.Navigation, item.Revision - 1);

                                        var summaryText = Differentiator.GetComparisionSummary(thisRev.Body, prevRev?.Body ?? "");

                                        if (summaryText.Length > 0)
                                        {
                                            html.Append(" - " + summaryText);
                                        }
                                    }
                                    html.Append("</li>");
                                }
                                html.Append("</ul>");

                                if (pageSelector && history.Count > 0 && history.First().PaginationCount > 1)
                                {
                                    html.Append(WikiUtility.GetPageSelector(refTag, history.First().PaginationCount, pageNumber, _queryString));
                                }
                            }

                            StoreMatch(function, pageContent, match.Value, html.ToString());
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "editlink": //(##EditLink(link text))
                        {
                            var linkText = function.Parameters.Get<String>("linkText");
                            StoreMatch(function, pageContent, match.Value, "<a href=\"" + WikiUtility.CleanFullURI($"/Page/Edit/{_page.Navigation}") + $"\">{linkText}</a>");
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    //injects an un-processed wiki body into the calling page.
                    case "inject": //(PageName)
                        {
                            var navigation = function.Parameters.Get<String>("pageName");

                            Page page = WikiUtility.GetPageFromPathInfo(navigation);
                            if (page != null)
                            {
                                var identifier = StoreMatch(function, pageContent, match.Value, page.Body);
                                pageContent.Replace($"{identifier}\n", $"{identifier}"); //Kill trailing newline.
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
                            var navigation = function.Parameters.Get<String>("pageName");

                            Page page = WikiUtility.GetPageFromPathInfo(navigation);
                            if (page != null)
                            {
                                var wikify = new Wikifier(_context, page, null, _queryString, _omitMatches.ToArray(), _nestLevel + 1);
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
                            var key = function.Parameters.Get<String>("key");
                            var value = function.Parameters.Get<String>("value");

                            if (_userVariables.ContainsKey(key))
                            {
                                _userVariables[key] = value;
                            }
                            else
                            {
                                _userVariables.Add(key, value);
                            }
                            var identifier = StoreMatch(function, pageContent, match.Value, string.Empty);
                            pageContent.Replace($"{identifier}\n", $"{identifier}"); //Kill trailing newline.
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "get":
                        {
                            var key = function.Parameters.Get<String>("key");

                            if (_userVariables.ContainsKey(key))
                            {
                                StoreMatch(function, pageContent, match.Value, _userVariables[key]);
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
                            var color = function.Parameters.Get<String>("color");
                            var text = function.Parameters.Get<String>("text");
                            StoreMatch(function, pageContent, match.Value, $"<font color=\"{color}\">{text}</font>");
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    //Associates tags with a page. These are saved with the page and can also be displayed.
                    case "tag": //##tag(pipe|seperated|list|of|tags)
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
                            string imageName = function.Parameters.Get<String>("name");
                            string alt = function.Parameters.Get<String>("alttext", imageName);
                            int scale = function.Parameters.Get<int>("scale");

                            string navigation = _page.Navigation;
                            if (imageName.Contains("/"))
                            {
                                //Allow loading attacehd images from other pages.
                                int slashIndex = imageName.IndexOf("/");
                                navigation = WikiUtility.CleanPartialURI(imageName.Substring(0, slashIndex));
                                imageName = imageName.Substring(slashIndex + 1);
                            }

                            if (_revision != null)
                            {
                                string link = $"/File/Image/{navigation}/{WikiUtility.CleanPartialURI(imageName)}/r/{_revision}";
                                string image = $"<a href=\"{link}\" target=\"_blank\"><img src=\"{link}?Scale={scale}\" border=\"0\" alt=\"{alt}\" /></a>";
                                StoreMatch(function, pageContent, match.Value, image);
                            }
                            else
                            {
                                string link = $"/File/Image/{navigation}/{WikiUtility.CleanPartialURI(imageName)}";
                                string image = $"<a href=\"{link}\" target=\"_blank\"><img src=\"{link}?Scale={scale}\" border=\"0\" alt=\"{alt}\" /></a>";
                                StoreMatch(function, pageContent, match.Value, image);
                            }
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Displays an file download link
                    case "file": //##file(Name | Alt-Text | [optional display file size] true/false)
                        {
                            int pageId = _page.Id;

                            string fileName = function.Parameters.Get<String>("name");
                            string navigation = _page.Navigation;
                            if (fileName.Contains("/"))
                            {
                                //Allow loading attacehd files from other pages.
                                int slashIndex = fileName.IndexOf("/");
                                navigation = WikiUtility.CleanPartialURI(fileName.Substring(0, slashIndex));

                                var page = PageRepository.GetPageInfoByNavigation(navigation);
                                if (page == null)
                                {
                                    StoreError(pageContent, match.Value, $"Page [{navigation}] not found for file [{fileName}]");
                                    break;
                                }

                                pageId = page.Id;
                                fileName = fileName.Substring(slashIndex + 1);
                            }

                            var attachment = PageFileRepository.GetPageFileInfoByPageIdPageRevisionAndName(pageId, fileName);
                            if (attachment != null)
                            {
                                string alt = function.Parameters.Get<String>("linkText", fileName);

                                if (function.Parameters.Get<bool>("showSize"))
                                {
                                    alt += $" ({attachment.FriendlySize})";
                                }

                                if (_revision != null)
                                {
                                    string link = $"/File/Binary/{navigation}/{WikiUtility.CleanPartialURI(fileName)}/r/{_revision}";
                                    string image = $"<a href=\"{link}\">{alt}</a>";
                                    StoreMatch(function, pageContent, match.Value, image);
                                }
                                else
                                {
                                    string link = $"/File/Binary/{navigation}/{WikiUtility.CleanPartialURI(fileName)}";
                                    string image = $"<a href=\"{link}\">{alt}</a>";
                                    StoreMatch(function, pageContent, match.Value, image);
                                }
                            }

                            StoreError(pageContent, match.Value, $"File not found [{fileName}]");
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Creates a list of pages that have been recently modified.
                    case "recentlymodified": //##RecentlyModified(TopCount)
                        {
                            string styleName = function.Parameters.Get<String>("styleName").ToLower();
                            var takeCount = function.Parameters.Get<int>("top");

                            var pages = PageRepository.GetTopRecentlyModifiedPagesInfo(takeCount)
                                .OrderByDescending(o => o.ModifiedDate).ThenBy(o => o.Name).ToList();

                            var html = new StringBuilder();

                            if (pages.Count() > 0)
                            {
                                html.Append("<ul>");
                                foreach (var page in pages)
                                {
                                    html.Append($"<li><a href=\"/{page.Navigation}\">{page.Name}</a>");

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
                    //Creates a glossary of pages with the specified comma seperated tags.
                    case "tagglossary":
                        {
                            string glossaryName = "glossary_" + (new Random()).Next(0, 1000000).ToString();
                            var tags = function.Parameters.GetList<string>("pageTags");

                            string styleName = function.Parameters.Get<String>("styleName").ToLower();
                            var topCount = function.Parameters.Get<int>("top");
                            var pages = PageTagRepository.GetPageInfoByTags(tags).Take(topCount).OrderBy(o => o.Name).ToList();
                            var html = new StringBuilder();
                            var alphabet = pages.Select(p => p.Name.Substring(0, 1).ToUpper()).Distinct();

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
                                    foreach (var page in pages.Where(p => p.Name.ToLower().StartsWith(alpha.ToLower())))
                                    {
                                        html.Append("<li><a href=\"/" + page.Navigation + "\">" + page.Name + "</a>");

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
                    //Creates a glossary by searching page's body text for the specified comma seperated list of words.
                    case "textglossary":
                        {
                            string glossaryName = "glossary_" + (new Random()).Next(0, 1000000).ToString();
                            var searchStrings = function.Parameters.Get<string>("searchPhrase").Split(" ", StringSplitOptions.RemoveEmptyEntries);
                            var topCount = function.Parameters.Get<int>("top");

                            var searchTerms = (from o in searchStrings
                                               select new PageToken
                                               {
                                                   Token = o,
                                                   DoubleMetaphone = o.ToDoubleMetaphone()
                                               }).ToList();

                            var pages = PageRepository.PageSearch(searchTerms).Take(topCount).OrderBy(o => o.Name).ToList();
                            var html = new StringBuilder();
                            var alphabet = pages.Select(p => p.Name.Substring(0, 1).ToUpper()).Distinct();
                            string styleName = function.Parameters.Get<String>("styleName").ToLower();

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
                                    foreach (var page in pages.Where(p => p.Name.ToLower().StartsWith(alpha.ToLower())))
                                    {
                                        html.Append("<li><a href=\"/" + page.Navigation + "\">" + page.Name + "</a>");

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
                            string styleName = function.Parameters.Get<String>("styleName").ToLower();
                            string refTag = GenerateQueryToken();
                            int pageNumber = int.Parse(_queryString[refTag].ToString().IsNullOrEmpty("1"));
                            var pageSize = function.Parameters.Get<int>("pageSize");
                            var pageSelector = function.Parameters.Get<bool>("pageSelector");
                            var allowFuzzyMatching = function.Parameters.Get<bool>("allowFuzzyMatching");
                            var tokens = function.Parameters.Get<string>("searchPhrase").Split(" ", StringSplitOptions.RemoveEmptyEntries);

                            var searchTerms = (from o in tokens
                                               select new PageToken
                                               {
                                                   Token = o,
                                                   DoubleMetaphone = o.ToDoubleMetaphone()
                                               }).ToList();

                            var pages = PageRepository.PageSearchPaged(searchTerms, pageNumber, pageSize, allowFuzzyMatching);
                            var html = new StringBuilder();

                            if (pages.Count() > 0)
                            {
                                html.Append("<ul>");

                                foreach (var page in pages)
                                {
                                    html.Append("<li><a href=\"/" + page.Navigation + "\">" + page.Name + "</a>");

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

                            if (pageSelector && pages.Count > 0 && pages.First().PaginationCount > 1)
                            {
                                html.Append(WikiUtility.GetPageSelector(refTag, pages.First().PaginationCount, pageNumber, _queryString));
                            }

                            StoreMatch(function, pageContent, match.Value, html.ToString());
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Creates a list of pages by searching the page tags.
                    case "taglist":
                        {
                            string styleName = function.Parameters.Get<String>("styleName").ToLower();
                            var topCount = function.Parameters.Get<int>("top");
                            var tags = function.Parameters.GetList<string>("pageTags");

                            var pages = PageTagRepository.GetPageInfoByTags(tags).Take(topCount).OrderBy(o => o.Name).ToList();
                            var html = new StringBuilder();

                            if (pages.Count() > 0)
                            {
                                html.Append("<ul>");

                                foreach (var page in pages)
                                {
                                    html.Append("<li><a href=\"/" + page.Navigation + "\">" + page.Name + "</a>");

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

                            int pageNumber = int.Parse(_queryString[refTag].ToString().IsNullOrEmpty("1"));
                            var pageSize = function.Parameters.Get<int>("pageSize");
                            var pageSelector = function.Parameters.Get<bool>("pageSelector");
                            string styleName = function.Parameters.Get<String>("styleName").ToLower();
                            var html = new StringBuilder();

                            var pages = PageRepository.GetSimilarPagesPaged(_page.Id, pageNumber, pageSize);

                            if (styleName == "list")
                            {
                                html.Append("<ul>");
                                foreach (var page in pages)
                                {
                                    html.Append($"<li><a href=\"/{page.Navigation}\">{page.Name}</a>");
                                }
                                html.Append("</ul>");
                            }
                            else if (styleName == "flat")
                            {
                                foreach (var page in pages)
                                {
                                    if (html.Length > 0) html.Append(" | ");
                                    html.Append($"<a href=\"/{page.Navigation}\">{page.Name}</a>");
                                }
                            }
                            else if (styleName == "full")
                            {
                                html.Append("<ul>");
                                foreach (var page in pages)
                                {
                                    html.Append($"<li><a href=\"/{page.Navigation}\">{page.Name}</a> - {page.Description}");
                                }
                                html.Append("</ul>");
                            }

                            if (pageSelector && pages.Count > 0 && pages.First().PaginationCount > 1)
                            {
                                html.Append(WikiUtility.GetPageSelector(refTag, pages.First().PaginationCount, pageNumber, _queryString));
                            }

                            StoreMatch(function, pageContent, match.Value, html.ToString());
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Displays a list of other related pages based incoming links.
                    case "related": //##related
                        {
                            string refTag = GenerateQueryToken();

                            int pageNumber = int.Parse(_queryString[refTag].ToString().IsNullOrEmpty("1"));
                            var pageSize = function.Parameters.Get<int>("pageSize");
                            var pageSelector = function.Parameters.Get<bool>("pageSelector");
                            string styleName = function.Parameters.Get<String>("styleName").ToLower();
                            var html = new StringBuilder();

                            var pages = PageRepository.GetRelatedPagesPaged(_page.Id, pageNumber, pageSize);

                            if (styleName == "list")
                            {
                                html.Append("<ul>");
                                foreach (var page in pages)
                                {
                                    html.Append($"<li><a href=\"/{page.Navigation}\">{page.Name}</a>");
                                }
                                html.Append("</ul>");
                            }
                            else if (styleName == "flat")
                            {
                                foreach (var page in pages)
                                {
                                    if (html.Length > 0) html.Append(" | ");
                                    html.Append($"<a href=\"/{page.Navigation}\">{page.Name}</a>");
                                }
                            }
                            else if (styleName == "full")
                            {
                                html.Append("<ul>");
                                foreach (var page in pages)
                                {
                                    html.Append($"<li><a href=\"/{page.Navigation}\">{page.Name}</a> - {page.Description}");
                                }
                                html.Append("</ul>");
                            }

                            if (pageSelector && pages.Count > 0 && pages.First().PaginationCount > 1)
                            {
                                html.Append(WikiUtility.GetPageSelector(refTag, pages.First().PaginationCount, pageNumber, _queryString));
                            }

                            StoreMatch(function, pageContent, match.Value, html.ToString());
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Displays the date and time that the current page was last modified.
                    case "lastmodified":
                        {
                            DateTime lastModified = DateTime.MinValue;
                            lastModified = _page.ModifiedDate;
                            if (lastModified != DateTime.MinValue)
                            {
                                var localized = _context.LocalizeDateTime(lastModified);
                                StoreMatch(function, pageContent, match.Value, $"{localized.ToShortDateString()} {localized.ToShortTimeString()}");
                            }
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Displays the date and time that the current page was created.
                    case "created":
                        {
                            DateTime createdDate = DateTime.MinValue;
                            createdDate = _page.CreatedDate;
                            if (createdDate != DateTime.MinValue)
                            {
                                var localized = _context.LocalizeDateTime(createdDate);
                                StoreMatch(function, pageContent, match.Value, $"{localized.ToShortDateString()} {localized.ToShortTimeString()}");
                            }
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Displays the version of the wiki.
                    case "appversion":
                        {
                            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                            StoreMatch(function, pageContent, match.Value, version);
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Displays the name of the current page.
                    case "name":
                        {
                            StoreMatch(function, pageContent, match.Value, _page.Name);
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Displays the name of the current page in title form.
                    case "title":
                        {
                            StoreMatch(function, pageContent, match.Value, $"<h1>{_page.Name}</h1>");
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
        private void TransformPostProcess(StringBuilder pageContent)
        {
            //Remove the last "(\#\#[\w-]+)" if you start to have matching problems:
            Regex rgx = new Regex(@"(\#\#[\w-]+\(\))|(##|{{|@@)([a-zA-Z_\s{][a-zA-Z0-9_\s{]*)\(((?<BR>\()|(?<-BR>\))|[^()]*)+\)|(\#\#[\w-]+)", RegexOptions.IgnoreCase);
            var matches = WikiUtility.OrderMatchesByLengthDescending(rgx.Matches(pageContent.ToString()));

            foreach (var match in matches)
            {
                FunctionCallInstance function;

                try
                {
                    function = FunctionParser.ParseFunctionCallInfo(match, out int matchEndIndex);
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
                            string styleName = function.Parameters.Get<String>("styleName").ToLower();
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
                            string seedTag = function.Parameters.Get<String>("pageTag");
                            string cloudHtml = WikiUtility.BuildTagCloud(seedTag);
                            StoreMatch(function, pageContent, match.Value, cloudHtml);
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    case "searchcloud":
                        {
                            var tokens = function.Parameters.Get<string>("searchPhrase").Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList();
                            string cloudHtml = WikiUtility.BuildSearchCloud(tokens);
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

        private void StoreCriticalError(string exceptionText)
        {
            ErrorCount++;

            var html = new StringBuilder();
            html.Append("<div class=\"card bg-warning mb-3\">");
            html.Append($"<div class=\"card-header\"><strong>Wiki Parser Exception</strong></div>");
            html.Append("<div class=\"card-body\">");
            html.Append($"<p class=\"card-text\">{exceptionText}");
            html.Append("</p>");
            html.Append("</div>");
            html.Append("</div>");
            ProcessedBody = html.ToString();
        }

        private string StoreError(StringBuilder pageContent, string match, string value)
        {
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

        private string StoreMatch(WikiMatchType matchType, StringBuilder pageContent, string match, string value, bool allowNestedDecode = true)
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

        private string StoreMatch(FunctionCallInstance function, StringBuilder pageContent, string match, string value, bool allowNestedDecode = true)
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

        private void TransformWhitespace(StringBuilder pageContent)
        {
            string identifier = $"<!--{Guid.NewGuid()}-->";

            //Replace new-lines with single character new line:
            pageContent.Replace("\r\n", "\n");

            //Replace new-lines with an identifer so we can identify the places we are going to introduce line-breaks:
            pageContent.Replace("\n", identifier);

            //Replace any consecutive to-be-line-breaks that we are introducing with single line-break identifers.
            pageContent.Replace($"{identifier}{identifier}", identifier);

            //Swap in the real line-breaks.
            pageContent.Replace(identifier, "<br />");
        }

        /// <summary>
        /// Replaces HTML where we are transforming the entire line, such as "*this will be bold" - > "<b>this will be bold</b>
        /// </summary>
        /// <param name="regex"></param>
        /// <param name="htmlTag"></param>
        void ReplaceWholeLineHTMLMarker(StringBuilder pageContent, string mark, string htmlTag, bool escape)
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

            Regex rgx = new Regex($"^{marker}.*?\n", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            var matches = WikiUtility.OrderMatchesByLengthDescending(rgx.Matches(pageContent.ToString()));
            //We roll-through these matches in reverse order because we are replacing by position. We don't move the earlier positions by replacing from the bottom up.
            foreach (var match in matches)
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
            _queryTokenState = Security.Sha256(Security.EncryptString(_queryTokenState, _queryTokenState));
            return $"H{Security.Crc32(_queryTokenState)}";
        }

        void ReplaceInlineHTMLMarker(StringBuilder pageContent, string mark, string htmlTag, bool escape)
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

            Regex rgx = new Regex($@"{marker}([\S\s]*?){marker}", RegexOptions.IgnoreCase);
            var matches = WikiUtility.OrderMatchesByLengthDescending(rgx.Matches(pageContent.ToString()));
            foreach (var match in matches)
            {
                string value = match.Value.Substring(mark.Length, match.Value.Length - (mark.Length * 2));

                StoreMatch(WikiMatchType.Formatting, pageContent, match.Value, $"<{htmlTag}>{value}</{htmlTag}>");
            }
        }
    }
}
