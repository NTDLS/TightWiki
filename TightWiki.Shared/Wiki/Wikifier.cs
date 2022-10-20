using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using TightWiki.Shared.Library;
using TightWiki.Shared.Models;
using TightWiki.Shared.Models.Data;
using TightWiki.Shared.Repository;
using TightWiki.Shared.Wiki.MethodCall;
using static TightWiki.Shared.Library.Constants;
using static TightWiki.Shared.Wiki.MethodCall.Singletons;

namespace TightWiki.Shared.Wiki
{
    public class Wikifier
    {
        private const string SoftBreak = "<!--SoftBreak-->"; //These will remain as \r\n in the final HTML.
        private const string HardBreak = "<!--HardBreak-->"; //These will remain as <br /> in the final HTML.

        public List<string> ProcessingInstructions { get; private set; } = new List<string>();
        public string ProcessedBody { get; private set; }
        public List<string> Tags { get; private set; } = new List<string>();
        public Dictionary<string, MatchSet> Matches { get; private set; } = new Dictionary<string, MatchSet>();

        private int _matchesPerIteration = 0;
        private readonly string _tocName = "TOC_" + (new Random()).Next(0, 1000000).ToString();
        private readonly List<TOCTag> _tocTags = new List<TOCTag>();
        private readonly Page _page;
        private readonly int? _revision;
        readonly NameValueCollection _queryString;
        private readonly StateContext _context;
        private readonly int _nestLevel;

        /// <summary>
        /// When matches are omitted, the entire match will be removed from the resulting wiki text.
        /// </summary>
        private List<WikiMatchType> _omitMatches = new List<WikiMatchType>();

        public Wikifier(StateContext context, Page page, int? revision = null, NameValueCollection queryString = null, WikiMatchType []omitMatches = null, int nestLevel = 0)
        {
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

            Transform();
        }

        public List<WeightedToken> ParsePageTokens()
        {
            var allTokens = new List<WeightedToken>();

            allTokens.AddRange(WikiUtility.ParsePageTokens(ProcessedBody, 1));
            allTokens.AddRange(WikiUtility.ParsePageTokens(_page.Description, 2));
            allTokens.AddRange(WikiUtility.ParsePageTokens(_page.Name, 3));

            allTokens = allTokens.GroupBy(o => o.Token).Select(o => new WeightedToken
            {
                Token = o.Key,
                Weight = o.Sum(g => g.Weight)
            }).ToList();

            return allTokens;
        }

        private void Transform()
        {
            var pageContent = new StringBuilder(_page.Body);

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
                        pageContent.Replace(v.Key, String.Empty);
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
                html.Append("<div class=\"panel panel-warning\">");
                html.Append($"<div class=\"panel-heading\">Viewing a historical revision</div>");
                html.Append($"<div class=\"panel-body\">You are viewing revision {_revision:0} of \"{_page.Name}\" modified by \"{revision.ModifiedByUserName}\" on {revision.ModifiedDate}.<br />");
                html.Append($"<a href=\"/{_page.Navigation}\">View the latest revision {pageInfo.Revision:0}.</a></div></div><br />");
                pageContent.Insert(0, html);
            }

            pageContent.Replace(SoftBreak, "\r\n");
            pageContent.Replace(HardBreak, "<br />");

            ProcessedBody = pageContent.ToString();
        }

        public int TransformAll(StringBuilder pageContent)
        {
            _matchesPerIteration = 0;

            TransformBlocks(pageContent);
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
                            pageContent.Replace(v.Key, String.Empty);
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
        }

        /// <summary>6
        /// Transform inline and multi-line literal blocks. These are blocks where the content will not be wikified and contain code that is encoded to display verbatim on the page.
        /// </summary>
        /// <param name="pageContent"></param>
        private void TransformLiterals(StringBuilder pageContent)
        {
            //TODO: May need to do the same thing we did with TransformBlocks() to match all these if they need to be nested.


            //Transform literal strings, even encodes HTML so that it displays verbatim.
            Regex rgx = new Regex(@"\[\{\{([\S\s]*?)\}\}\]", RegexOptions.IgnoreCase);
            var matches = WikiUtility.OrderMatchesByLengthDescending(rgx.Matches(pageContent.ToString()));
            foreach (var match in matches)
            {
                string value = match.Value.Substring(3, match.Value.Length - 6);
                value = HttpUtility.HtmlEncode(value);
                StoreMatch(WikiMatchType.Literal, pageContent, match.Value, value.Replace("\r", "").Trim().Replace("\n", "<br />"), false);
            }
        }

        /// <summary>
        /// Matching nested blocks with regex was hell, I escaped with a loop. ¯\_(ツ)_/¯
        /// </summary>
        /// <param name="pageContent"></param>
        private void TransformBlocks(StringBuilder pageContent)
        {
            var content = pageContent.ToString();

            while (true)
            {
                int startPos = content.LastIndexOf("{{{");
                if (startPos < 0)
                {
                    break;
                }
                int endPos = content.IndexOf("}}}", startPos);

                string rawBlock = content.Substring(startPos, (endPos - startPos) + 3);
                var transformBlock = new StringBuilder(rawBlock);
                TransformBlock(transformBlock);
                content = content.Replace(rawBlock, transformBlock.ToString());
            }

            pageContent.Clear();
            pageContent.Append(content);
        }

        /// <summary>
        /// Transform blocks or sections of code, these are thinks like panels and alerts.
        /// </summary>
        /// <param name="pageContent"></param>
        private void TransformBlock(StringBuilder pageContent)
        {
            Regex rgx = new Regex(@"{{{([\S\s]*)}}}", RegexOptions.IgnoreCase);
            var matches = WikiUtility.OrderMatchesByLengthDescending(rgx.Matches(pageContent.ToString()));
            foreach (var match in matches)
            {
                int paramEndIndex = -1;

                MethodCallInstance method;

                try
                {
                    method = Singletons.ParseMethodCallInfo(match, out paramEndIndex, "PanelScope");
                }
                catch (Exception ex)
                {
                    StoreError(pageContent, match.Value, ex.Message);
                    continue;
                }

                string scopeBody = match.Value.Substring(paramEndIndex, (match.Value.Length - paramEndIndex) - 3).Trim();

                string boxType = method.Parameters.Get<String>("boxType");
                string title = method.Parameters.Get<String>("title");

                var html = new StringBuilder();

                switch (boxType.ToLower())
                {
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "code":
                        {
                            string language = method.Parameters.Get<string>("language");
                            if (string.IsNullOrEmpty(language))
                            {
                                html.Append($"<pre>");
                                if (!String.IsNullOrEmpty(title)) html.Append($"<strong>{title}</strong>{HardBreak}");
                                html.Append($"<code>{scopeBody.Replace("\r\n", "\n").Replace("\n", SoftBreak)}</code></pre>");
                            }
                            else
                            {
                                html.Append($"<pre class=\"language-{language}\">");
                                if (!String.IsNullOrEmpty(title)) html.Append($"<strong>{title}</strong>{HardBreak}");
                                html.Append($"<code>{scopeBody.Replace("\r\n", "\n").Replace("\n", SoftBreak)}</code></pre>");
                            }
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "bullets":
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
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "bullets-ordered":
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
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "alert":
                    case "alert-default":
                    case "alert-info":
                        {
                            if (!String.IsNullOrEmpty(title)) scopeBody = $"<h1>{title}</h1>{scopeBody}";
                            html.Append($"<div class=\"alert alert-info\">{scopeBody}.</div>");
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "alert-danger":
                        {
                            if (!String.IsNullOrEmpty(title)) scopeBody = $"<h1>{title}</h1>{scopeBody}";
                            html.Append($"<div class=\"alert alert-danger\">{scopeBody}.</div>");
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "alert-warning":
                        {
                            if (!String.IsNullOrEmpty(title)) scopeBody = $"<h1>{title}</h1>{scopeBody}";
                            html.Append($"<div class=\"alert alert-warning\">{scopeBody}.</div>");
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "alert-success":
                        {
                            if (!String.IsNullOrEmpty(title)) scopeBody = $"<h1>{title}</h1>{scopeBody}";
                            html.Append($"<div class=\"alert alert-success\">{scopeBody}.</div>");
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    case "jumbotron":
                        {
                            if (!String.IsNullOrEmpty(title)) scopeBody = $"<h1>{title}</h1>{scopeBody}";
                            html.Append($"<div class=\"jumbotron\">{scopeBody}</div>");
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "block":
                    case "block-default":
                        {
                            html.Append("<div class=\"panel panel-default\">");
                            html.Append($"<div class=\"panel-body\">{scopeBody}</div></div>");
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "block-primary":
                        {
                            html.Append("<div class=\"panel panel-primary\">");
                            html.Append($"<div class=\"panel-body\">{scopeBody}</div></div>");
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "block-success":
                        {
                            html.Append("<div class=\"panel panel-success\">");
                            html.Append($"<div class=\"panel-body\">{scopeBody}</div></div>");
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "block-info":
                        {
                            html.Append("<div class=\"panel panel-info\">");
                            html.Append($"<div class=\"panel-body\">{scopeBody}</div></div>");
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "block-warning":
                        {
                            html.Append("<div class=\"panel panel-warning\">");
                            html.Append($"<div class=\"panel-body\">{scopeBody}</div></div>");
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "block-danger":
                        {
                            html.Append("<div class=\"panel panel-danger\">");
                            html.Append($"<div class=\"panel-body\">{scopeBody}</div></div>");
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "panel":
                    case "panel-default":
                        {
                            html.Append("<div class=\"panel panel-default\">");
                            html.Append($"<div class=\"panel-heading\">{title}</div>");
                            html.Append($"<div class=\"panel-body\">{scopeBody}</div></div>");
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "panel-primary":
                        {
                            html.Append("<div class=\"panel panel-primary\">");
                            html.Append($"<div class=\"panel-heading\">{title}</div>");
                            html.Append($"<div class=\"panel-body\">{scopeBody}</div></div>");
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "panel-success":
                        {
                            html.Append("<div class=\"panel panel-success\">");
                            html.Append($"<div class=\"panel-heading\">{title}</div>");
                            html.Append($"<div class=\"panel-body\">{scopeBody}</div></div>");
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "panel-info":
                        {
                            html.Append("<div class=\"panel panel-info\">");
                            html.Append($"<div class=\"panel-heading\">{title}</div>");
                            html.Append($"<div class=\"panel-body\">{scopeBody}</div></div>");
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "panel-warning":
                        {
                            html.Append("<div class=\"panel panel-warning\">");
                            html.Append($"<div class=\"panel-heading\">{title}</div>");
                            html.Append($"<div class=\"panel-body\">{scopeBody}</div></div>");
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "panel-danger":
                        {
                            html.Append("<div class=\"panel panel-danger\">");
                            html.Append($"<div class=\"panel-heading\">{title}</div>");
                            html.Append($"<div class=\"panel-body\">{scopeBody}</div></div>");
                        }
                        break;
                }

                StoreMatch(WikiMatchType.Block, pageContent, match.Value, html.ToString());
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
                    string value = match.Value.Substring(headingMarkers, match.Value.Length - headingMarkers).Trim();

                    int fontSize = 8 - headingMarkers;
                    if (fontSize < 5) fontSize = 5;

                    string link = "<font size=\"" + fontSize + "\"><a name=\"" + tag + "\"><span class=\"WikiH" + (headingMarkers - 1).ToString() + "\">" + value + "</span></a></font>\r\n";
                    StoreMatch(WikiMatchType.Heading, pageContent, match.Value, link);
                    _tocTags.Add(new TOCTag(headingMarkers - 1, match.Index, tag, value));
                }
            }

            regEx = new StringBuilder();
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

            rgx = new Regex(regEx.ToString(), RegexOptions.IgnoreCase);
            matches = WikiUtility.OrderMatchesByLengthDescending(rgx.Matches(pageContent.ToString()));

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
                    StoreMatch(WikiMatchType.Heading, pageContent, match.Value, link);
                    _tocTags.Add(new TOCTag(headingMarkers - 1, match.Index, tag, value));
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

                if (page != null)
                {
                    if (explicitLinkText.Length == 0)
                    {
                        linkText = page.Name;
                    }
                    else
                    {
                        linkText = explicitLinkText;

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
                MethodCallInstance method;

                try
                {
                    method = Singletons.ParseMethodCallInfo(match, out int matchEndIndex);
                }
                catch (Exception ex)
                {
                    StoreError(pageContent, match.Value, ex.Message);
                    continue;
                }

                switch (method.Name.ToLower())
                {
                    //We check _nestLevel here because we dont want to include the processing instructions on any parent pages that are injecting this one.

                    //------------------------------------------------------------------------------------------------------------------------------
                    case "depreciate":
                        if (_nestLevel == 0)
                        {
                            ProcessingInstructions.Add(WikiInstruction.Depreciate);
                            pageContent.Insert(0, "<div class=\"alert alert-danger\">This page has been depreciate and will be deleted.</div>");
                        }
                        StoreMatch(WikiMatchType.Instruction, pageContent, match.Value, "");
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "protect":
                        {
                            if (_nestLevel == 0)
                            {
                                bool isSilent = method.Parameters.Get<bool>("isSilent");
                                ProcessingInstructions.Add(WikiInstruction.Protect);
                                if (isSilent == false)
                                {
                                    pageContent.Insert(0, "<div class=\"alert alert-info\">This page has been protected and can not be changed by non-moderators.</div>");
                                }
                            }
                            StoreMatch(WikiMatchType.Instruction, pageContent, match.Value, "");
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "template":
                        if (_nestLevel == 0)
                        {
                            ProcessingInstructions.Add(WikiInstruction.Template);
                            pageContent.Insert(0, "<div class=\"alert alert-info\">This page is a template and will not appear in indexes or glossaries.</div>");
                        }
                        StoreMatch(WikiMatchType.Instruction, pageContent, match.Value, "");
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "review":
                        if (_nestLevel == 0)
                        {
                            ProcessingInstructions.Add(WikiInstruction.Review);
                            pageContent.Insert(0, "<div class=\"alert alert-warning\">This page has been flagged for review, its content may be inaccurate.</div>");
                        }
                        StoreMatch(WikiMatchType.Instruction, pageContent, match.Value, "");
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "include":
                        if (_nestLevel == 0)
                        {
                            ProcessingInstructions.Add(WikiInstruction.Include);
                            pageContent.Insert(0, "<div class=\"alert alert-info\">This page is an include and will not appear in indexes or glossaries.</div>");
                        }
                        StoreMatch(WikiMatchType.Instruction, pageContent, match.Value, "");
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "draft":
                        if (_nestLevel == 0)
                        {
                            ProcessingInstructions.Add(WikiInstruction.Draft);
                            pageContent.Insert(0, "<div class=\"alert alert-warning\">This page is a draft and may contain incorrect information and/or experimental styling.</div>");
                        }
                        StoreMatch(WikiMatchType.Instruction, pageContent, match.Value, "");
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
                MethodCallInstance method;

                try
                {
                    method = ParseMethodCallInfo(match, out int matchEndIndex);
                }
                catch (Exception ex)
                {
                    StoreError(pageContent, match.Value, ex.Message);
                    continue;
                }

                switch (method.Name.ToLower())
                {
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "history":
                        {
                            int pageNumber = int.Parse((_queryString.Get("page") ?? "1"));

                            string view = method.Parameters.Get<String>("View").ToLower();
                            var pageSize = method.Parameters.Get<int>("pageSize");
                            var pageSelector = method.Parameters.Get<bool>("pageSelector");
                            var history = PageRepository.GetPageRevisionHistoryInfoByNavigation(_page.Navigation, pageNumber, pageSize);
                            var html = new StringBuilder();

                            if (history.Count() > 0)
                            {
                                html.Append("<ul>");
                                foreach (var item in history)
                                {
                                    html.Append($"<li><a href=\"/{item.Navigation}/r/{item.Revision}\">{item.Revision} by {item.ModifiedByUserName} on {item.ModifiedDate}</a>");

                                    if (view == "full")
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

                                if (pageSelector)
                                {
                                    int paginationCount = history.First().PaginationCount;
                                    if (pageNumber > 1)
                                    {
                                        html.Append($"<a href=\"?Page=1\">&lt;&lt; First</a>");
                                        html.Append("&nbsp; | &nbsp;");
                                        html.Append($"<a href=\"?Page={pageNumber - 1}\"> &lt; Previous</a>");
                                    }
                                    else
                                    {
                                        html.Append("&lt;&lt; First &nbsp; | &nbsp; &lt; Previous");
                                    }
                                    html.Append("&nbsp; | &nbsp;");
                                    if (pageNumber < paginationCount)
                                    {
                                        html.Append($"<a href=\"?Page={pageNumber + 1}\"> Next &gt;</a>");
                                        html.Append(" &nbsp; | &nbsp;");
                                        html.Append($"<a href=\"?Page={paginationCount}\"> Last &gt;&gt;</a>");
                                    }
                                    else
                                    {
                                        html.Append("Next &gt; &nbsp; | &nbsp; Last &gt;&gt;");
                                    }
                                }
                            }

                            StoreMatch(method, pageContent, match.Value, html.ToString());

                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "editlink": //(##EditLink(link text))
                        {
                            var linkText = method.Parameters.Get<String>("linkText");
                            StoreMatch(method, pageContent, match.Value, "<a href=\"" + WikiUtility.CleanFullURI($"/Page/Edit/{_page.Navigation}") + $"\">{linkText}</a>");
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    //Includes/injects a page by it's navigation link.
                    case "inject": //(PageName)
                        {
                            var navigation = method.Parameters.Get<String>("pageName");

                            Page page = WikiUtility.GetPageFromPathInfo(navigation);
                            if (page != null)
                            {
                                var wikify = new Wikifier(_context, page, null, _queryString, _omitMatches.ToArray(), _nestLevel +1);
                                StoreMatch(method, pageContent, match.Value, wikify.ProcessedBody);
                            }
                            else
                            {
                                StoreError(pageContent, match.Value, $"The include page was not found: [{navigation}]");
                            }
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Associates tags with a page. These are saved with the page and can also be displayed.
                    case "tag": //##tag(pipe|seperated|list|of|tags)
                        {
                            var tags = method.Parameters.GetList<string>("tags");
                            Tags.AddRange(tags);
                            Tags = Tags.Distinct().ToList();
                            StoreMatch(method, pageContent, match.Value, "");
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Displays an image that is attached to the page.
                    case "image": //##Image(Name, [optional:default=100]Scale, [optional:default=""]Alt-Text)
                        {
                            string imageName = method.Parameters.Get<String>("name");
                            string alt = method.Parameters.Get<String>("alttext", imageName);
                            int scale = method.Parameters.Get<int>("scale");

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
                                StoreMatch(method, pageContent, match.Value, image);
                            }
                            else
                            {
                                string link = $"/File/Image/{navigation}/{WikiUtility.CleanPartialURI(imageName)}";
                                string image = $"<a href=\"{link}\" target=\"_blank\"><img src=\"{link}?Scale={scale}\" border=\"0\" alt=\"{alt}\" /></a>";
                                StoreMatch(method, pageContent, match.Value, image);
                            }
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Displays an file download link
                    case "file": //##file(Name | Alt-Text | [optional display file size] true/false)
                        {
                            int pageId = _page.Id;

                            string fileName = method.Parameters.Get<String>("name");
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
                                string alt = method.Parameters.Get<String>("linkText", fileName);

                                if (method.Parameters.Get<bool>("showSize"))
                                {
                                    alt += $" ({attachment.FriendlySize})";
                                }

                                if (_revision != null)
                                {
                                    string link = $"/File/Binary/{navigation}/{WikiUtility.CleanPartialURI(fileName)}/r/{_revision}";
                                    string image = $"<a href=\"{link}\">{alt}</a>";
                                    StoreMatch(method, pageContent, match.Value, image);
                                }
                                else
                                {
                                    string link = $"/File/Binary/{navigation}/{WikiUtility.CleanPartialURI(fileName)}";
                                    string image = $"<a href=\"{link}\">{alt}</a>";
                                    StoreMatch(method, pageContent, match.Value, image);
                                }
                            }

                            StoreError(pageContent, match.Value, $"File not found [{fileName}]");
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Displays a list of files attached to the page.
                    case "files": //##Files()
                        {
                            var files = PageFileRepository.GetPageFilesInfoByPageIdAndPageRevision(_page.Id);

                            var html = new StringBuilder();

                            if (files.Count() > 0)
                            {
                                html.Append("<ul>");
                                foreach (var file in files)
                                {
                                    if (_revision != null)
                                    {
                                        html.Append($"<li><a href=\"/File/Binary/{_page.Navigation}/{WikiUtility.CleanPartialURI(file.Name)}/r/{_revision}\">{file.Name} ({file.FriendlySize})</a>");
                                    }
                                    else
                                    {
                                        html.Append($"<li><a href=\"/File/Binary/{_page.Navigation}/{WikiUtility.CleanPartialURI(file.Name)}\">{file.Name} ({file.FriendlySize})</a>");
                                    }
                                    html.Append("</li>");
                                }
                                html.Append("</ul>");
                            }

                            StoreMatch(method, pageContent, match.Value, html.ToString());
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Creates a list of pages that have been recently modified.
                    case "recentlymodified": //##RecentlyModified(TopCount)
                        {
                            string view = method.Parameters.Get<String>("View").ToLower();
                            var takeCount = method.Parameters.Get<int>("top");

                            var pages = PageRepository.GetTopRecentlyModifiedPagesInfo(takeCount)
                                .OrderByDescending(o => o.ModifiedDate).ThenBy(o => o.Name).ToList();

                            var html = new StringBuilder();

                            if (pages.Count() > 0)
                            {
                                html.Append("<ul>");
                                foreach (var page in pages)
                                {
                                    html.Append($"<li><a href=\"/{page.Navigation}\">{page.Name}</a>");

                                    if (view == "full")
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

                            StoreMatch(method, pageContent, match.Value, html.ToString());
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Creates a glossary of pages with the specified comma seperated tags.
                    case "tagglossary":
                        {
                            string glossaryName = "glossary_" + (new Random()).Next(0, 1000000).ToString();
                            var tags = method.Parameters.GetList<string>("tags");

                            string view = method.Parameters.Get<String>("View").ToLower();
                            var topCount = method.Parameters.Get<int>("top");
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

                                        if (view == "full")
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

                            StoreMatch(method, pageContent, match.Value, html.ToString());
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Creates a glossary by searching page's body text for the specified comma seperated list of words.
                    case "textglossary":
                        {
                            string glossaryName = "glossary_" + (new Random()).Next(0, 1000000).ToString();
                            var searchStrings = method.Parameters.GetList<string>("tokens");
                            var topCount = method.Parameters.Get<int>("top");
                            var pages = PageTagRepository.GetPageInfoByTokens(searchStrings).Take(topCount).OrderBy(o => o.Name).ToList();
                            var html = new StringBuilder();
                            var alphabet = pages.Select(p => p.Name.Substring(0, 1).ToUpper()).Distinct();
                            string view = method.Parameters.Get<String>("View").ToLower();

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

                                        if (view == "full")
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

                            StoreMatch(method, pageContent, match.Value, html.ToString());
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Creates a list of pages by searching the page body for the specified text.
                    case "textlist":
                        {
                            string view = method.Parameters.Get<String>("View").ToLower();
                            var topCount = method.Parameters.Get<int>("top");
                            var tags = method.Parameters.GetList<string>("tags");
                            var pages = PageTagRepository.GetPageInfoByTokens(tags).Take(topCount).OrderBy(o => o.Name).ToList();
                            var html = new StringBuilder();

                            if (pages.Count() > 0)
                            {
                                html.Append("<ul>");

                                foreach (var page in pages)
                                {
                                    html.Append("<li><a href=\"/" + page.Navigation + "\">" + page.Name + "</a>");

                                    if (view == "full")
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

                            StoreMatch(method, pageContent, match.Value, html.ToString());
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Creates a list of pages by searching the page tags.
                    case "taglist":
                        {
                            string view = method.Parameters.Get<String>("View").ToLower();
                            var topCount = method.Parameters.Get<int>("top");
                            var tags = method.Parameters.GetList<string>("tags");

                            var pages = PageTagRepository.GetPageInfoByTags(tags).Take(topCount).OrderBy(o => o.Name).ToList();
                            var html = new StringBuilder();

                            if (pages.Count() > 0)
                            {
                                html.Append("<ul>");

                                foreach (var page in pages)
                                {
                                    html.Append("<li><a href=\"/" + page.Navigation + "\">" + page.Name + "</a>");

                                    if (view == "full")
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

                            StoreMatch(method, pageContent, match.Value, html.ToString());
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Displays a list of other related pages based on tags.
                    case "related": //##related
                        {
                            string view = method.Parameters.Get<String>("View").ToLower();
                            var html = new StringBuilder();
                            var topCount = method.Parameters.Get<int>("top");
                            var pages = PageRepository.GetRelatedPages(_page.Id).OrderBy(o => o.Name).Take(topCount).ToList();

                            if (view == "list")
                            {
                                html.Append("<ul>");
                                foreach (var page in pages)
                                {
                                    html.Append($"<li><a href=\"/{page.Navigation}\">{page.Name}</a>");
                                }
                                html.Append("</ul>");
                            }
                            else if (view == "flat")
                            {
                                foreach (var page in pages)
                                {
                                    if (html.Length > 0) html.Append(" | ");
                                    html.Append($"<a href=\"/{page.Navigation}\">{page.Name}</a>");
                                }
                            }
                            else if (view == "full")
                            {
                                html.Append("<ul>");
                                foreach (var page in pages)
                                {
                                    html.Append($"<li><a href=\"/{page.Navigation}\">{page.Name}</a> - {page.Description}");
                                }
                                html.Append("</ul>");
                            }

                            StoreMatch(method, pageContent, match.Value, html.ToString());
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
                                StoreMatch(method, pageContent, match.Value, lastModified.ToShortDateString());
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
                                StoreMatch(method, pageContent, match.Value, createdDate.ToShortDateString());
                            }
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Displays the version of the wiki.
                    case "appversion":
                        {
                            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                            StoreMatch(method, pageContent, match.Value, version);
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Displays the name of the current page.
                    case "name":
                        {
                            StoreMatch(method, pageContent, match.Value, _page.Name);
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Displays the name of the current page in title form.
                    case "title":
                        {
                            StoreMatch(method, pageContent, match.Value, $"<h1>{_page.Name}</h1>");
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Inserts empty lines into the page.
                    case "br":
                    case "nl":
                    case "newline": //##NewLine([optional:default=1]count)
                        {
                            int count = method.Parameters.Get<int>("Count");
                            for (int i = 0; i < count; i++)
                            {
                                StoreMatch(method, pageContent, match.Value, $"<br />");
                            }
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Displays the navigation text for the current page.
                    case "navigation":
                        {
                            string navigation = _page.Navigation;
                            if (navigation != string.Empty)
                            {
                                StoreMatch(method, pageContent, match.Value, navigation);
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
                MethodCallInstance method;

                try
                {
                    method = Singletons.ParseMethodCallInfo(match, out int matchEndIndex);
                }
                catch (Exception ex)
                {
                    StoreError(pageContent, match.Value, ex.Message);
                    continue;
                }

                switch (method.Name.ToLower())
                {
                    //------------------------------------------------------------------------------------------------------------------------------
                    //Displays a tag link list.
                    case "tags": //##tags
                        {
                            string view = method.Parameters.Get<String>("View").ToLower();
                            var html = new StringBuilder();

                            if (view == "list")
                            {
                                html.Append("<ul>");
                                foreach (var tag in Tags)
                                {
                                    html.Append($"<li><a href=\"/Tag/Browse/{tag}\">{tag}</a>");
                                }
                                html.Append("</ul>");
                            }
                            else if (view == "flat")
                            {
                                foreach (var tag in Tags)
                                {
                                    if (html.Length > 0) html.Append(" | ");
                                    html.Append($"<a href=\"/Tag/Browse/{tag}\">{tag}</a>");
                                }
                            }

                            StoreMatch(method, pageContent, match.Value, html.ToString());
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    case "tagcloud":
                        {
                            string seedTag = method.Parameters.Get<String>("tag");
                            string cloudHtml = WikiUtility.BuildTagCloud(seedTag);
                            StoreMatch(method, pageContent, match.Value, cloudHtml);
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    case "searchcloud":
                        {
                            var tokens = method.Parameters.GetList<string>("tokens");
                            string cloudHtml = WikiUtility.BuildSearchCloud(tokens);
                            StoreMatch(method, pageContent, match.Value, cloudHtml);
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Diplays a table of contents for the page based on the header tags.
                    case "toc":
                        {
                            var html = new StringBuilder();

                            var tags = from t in _tocTags
                                       orderby t.StartingPosition
                                       select t;

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

                            StoreMatch(method, pageContent, match.Value, html.ToString());
                        }
                        break;
                }
            }
        }

        private void StoreError(StringBuilder pageContent, string match, string value)
        {
            _matchesPerIteration++;

            string identifier = Guid.NewGuid().ToString();

            var matchSet = new MatchSet()
            {
                Content = $"<i><font size=\"3\" color=\"#BB0000\">{{{value}}}</font></a>",
                AllowNestedDecode = false,
                MatchType = WikiMatchType.Error
            };

            Matches.Add(identifier, matchSet);
            pageContent.Replace(match, identifier);
        }

        private void StoreMatch(WikiMatchType matchType, StringBuilder pageContent, string match, string value, bool allowNestedDecode = true)
        {
            _matchesPerIteration++;

            string identifier = Guid.NewGuid().ToString();

            var matchSet = new MatchSet()
            {
                MatchType = matchType,
                Content = value,
                AllowNestedDecode = allowNestedDecode
            };

            Matches.Add(identifier, matchSet);
            pageContent.Replace(match, identifier);
        }

        private void StoreMatch(MethodCallInstance method, StringBuilder pageContent, string match, string value, bool allowNestedDecode = true)
        {
            _matchesPerIteration++;

            string identifier = Guid.NewGuid().ToString();

            var matchSet = new MatchSet()
            {
                MatchType =  WikiMatchType.Function,
                Content = value,
                Method = method,
                AllowNestedDecode = allowNestedDecode
            };

            Matches.Add(identifier, matchSet);
            pageContent.Replace(match, identifier);
        }


        private void TransformWhitespace(StringBuilder pageContent)
        {
            string identifier = Guid.NewGuid().ToString();

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
            string marker = String.Empty;
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

        void ReplaceInlineHTMLMarker(StringBuilder pageContent, string mark, string htmlTag, bool escape)
        {
            string marker = String.Empty;
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

            Regex rgx = new Regex($@"{marker}.*?{marker}", RegexOptions.IgnoreCase);
            var matches = WikiUtility.OrderMatchesByLengthDescending(rgx.Matches(pageContent.ToString()));
            foreach (var match in matches)
            {
                string value = match.Value.Substring(mark.Length, match.Value.Length - (mark.Length * 2));

                StoreMatch(WikiMatchType.Formatting, pageContent, match.Value, $"<{htmlTag}>{value}</{htmlTag}>");
            }
        }
    }
}
