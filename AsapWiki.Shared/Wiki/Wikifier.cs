using AsapWiki.Shared.Classes;
using AsapWiki.Shared.Models;
using AsapWiki.Shared.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using static AsapWiki.Shared.Constants;

namespace AsapWiki.Shared.Wiki
{
    public class Wikifier
    {
        public List<string> ProcessingInstructions { get; private set; } = new List<string>();
        public string ProcessedBody { get; private set; }
        public List<string> Tags { get; private set; } = new List<string>();
        public Dictionary<string, MatchSet> Matches { get; private set; } = new Dictionary<string, MatchSet>();

        private int _matchesPerIteration = 0;
        private readonly string _tocName = "TOC_" + (new Random()).Next(0, 1000000).ToString();
        private readonly List<TOCTag> _tocTags = new List<TOCTag>();
        private Page _page;
        private readonly StateContext _context;

        public Wikifier(StateContext context, Page page)
        {
            _page = page;
            Matches = new Dictionary<string, MatchSet>();
            _context = context;

            Transform();
        }

        public Wikifier(Page page)
        {
            _page = page;
            Matches = new Dictionary<string, MatchSet>();
            _context = null; //Not being called from a webpage.

            Transform();
        }


        public List<WeightedToken> ParsePageTokens()
        {
            return Utility.ParsePageTokens(ProcessedBody);
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
                    pageContent.Replace(v.Key, v.Value.Content);
                }
            } while (length != pageContent.Length);

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
                        pageContent.Replace(v.Key, v.Value.Content);
                    }
                }
            } while (length != pageContent.Length);

            return _matchesPerIteration;
        }

        private void StoreError(StringBuilder pageContent, string match, string value)
        {
            _matchesPerIteration++;

            string identifier = "{" + Guid.NewGuid().ToString() + "}";

            var matchSet = new MatchSet()
            {
                Content = $"<i><font size=\"3\" color=\"#BB0000\">{{{value}}}</font></a>",
                AllowNestedDecode = false
            };

            Matches.Add(identifier, matchSet);
            pageContent.Replace(match, identifier);
        }

        private void StoreMatch(StringBuilder pageContent, string match, string value, bool allowNestedDecode = true)
        {
            _matchesPerIteration++;

            string identifier = "{" + Guid.NewGuid().ToString() + "}";

            var matchSet = new MatchSet()
            {
                Content = value,
                AllowNestedDecode = allowNestedDecode
            };

            Matches.Add(identifier, matchSet);
            pageContent.Replace(match, identifier);
        }

        private void StoreMatch(StringBuilder pageContent, int startPosition, int length, string value, bool allowNestedDecode = true)
        {
            _matchesPerIteration++;

            string identifier = "{" + Guid.NewGuid().ToString() + "}";

            var matchSet = new MatchSet()
            {
                Content = value,
                AllowNestedDecode = allowNestedDecode
            };

            Matches.Add(identifier, matchSet);
            pageContent.Remove(startPosition, length);
            pageContent.Insert(startPosition, identifier);
        }

        private void TransformWhitespace(StringBuilder pageContent)
        {
            pageContent.Replace("\r\n", "\n");
            pageContent.Replace("\n", "<br />");
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
            var matches = OrderMatchesByLengthDescending(rgx.Matches(pageContent.ToString()));
            //We roll-through these matches in reverse order because we are replacing by position. We don't move the earlier positions by replacing from the bottom up.
            foreach (var match in matches)
            {
                string value = match.Value.Substring(mark.Length, match.Value.Length - mark.Length).Trim();
                var matchString = match.Value.Trim(); //We trim the match because we are matching to the end of the line which includes the \r\n, which we do not want to replace.
                StoreMatch(pageContent, matchString, $"<{htmlTag}>{value}</{htmlTag}> ");
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
            var matches = OrderMatchesByLengthDescending(rgx.Matches(pageContent.ToString()));
            foreach (var match in matches)
            {
                string value = match.Value.Substring(mark.Length, match.Value.Length - (mark.Length * 2));

                StoreMatch(pageContent, match.Value, $"<{htmlTag}>{value}</{htmlTag}>");
            }
        }

        /// <summary>
        /// Transform basic markup such as bold, italics, underline, etc. for single and multi-line.
        /// </summary>
        /// <param name="pageContent"></param>
        private void TransformMarkup(StringBuilder pageContent)
        {
            ReplaceWholeLineHTMLMarker(pageContent, "**", "strong", true); //Single line bold.
            ReplaceWholeLineHTMLMarker(pageContent, "__", "u", false); //Single line underline.
            ReplaceWholeLineHTMLMarker(pageContent, "//", "i", true); //Single line italics.
            ReplaceWholeLineHTMLMarker(pageContent, "!!", "mark", true); //Single line highlight.

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
            //Transform literal strings, even encodes HTML so that it displays verbatim.
            Regex rgx = new Regex(@"\[\{\{([\S\s]*?)\}\}\]", RegexOptions.IgnoreCase);
            var matches = OrderMatchesByLengthDescending(rgx.Matches(pageContent.ToString()));
            foreach (var match in matches)
            {
                string value = match.Value.Substring(3, match.Value.Length - 6);
                value = HttpUtility.HtmlEncode(value);
                StoreMatch(pageContent, match.Value, value.Replace("\r", "").Replace("\n", "<br />"), false);
            }
        }

        private int StartsWithHowMany(string value, char ch)
        {
            int count = 0;
            foreach (var c in value)
            {
                if (c == ch)
                {
                    count++;
                }
                else
                {
                    return count;
                }
            }

            return count;
        }

        /// <summary>
        /// Transform blocks or sections of code, these are thinks like panels and alerts.
        /// </summary>
        /// <param name="pageContent"></param>
        private void TransformBlocks(StringBuilder pageContent)
        {
            //Transform panels.
            Regex rgx = new Regex(@"\{\{\{\(([\S\s]*?)\}\}\}", RegexOptions.IgnoreCase);
            var matches = OrderMatchesByLengthDescending(rgx.Matches(pageContent.ToString()));
            foreach (var match in matches)
            {
                string value = match.Value.Substring(3, match.Value.Length - 6).Trim();

                int newlineIndex = value.IndexOf(')');

                if (newlineIndex > 0)
                {
                    string firstLine = value.Substring(0, newlineIndex + 1).Trim();
                    string content = value.Substring(newlineIndex + 1).Trim();
                    string boxType;
                    string title = String.Empty;
                    bool allowNestedDecode = true;
                    if (firstLine.StartsWith("(") && firstLine.EndsWith(")"))
                    {
                        firstLine = firstLine.Substring(1, firstLine.Length - 2);

                        //Parse box type and title.
                        int index = firstLine.IndexOf("|");
                        if (index > 0) //Do we have a title? Only applicable for some of the box types really...
                        {
                            title = firstLine.Substring(index + 1).Trim();
                            boxType = firstLine.Substring(0, index).Trim();
                        }
                        else
                        {
                            boxType = firstLine.Trim();
                        }

                        var html = new StringBuilder();

                        switch (boxType.ToLower())
                        {
                            case "bullets":
                                {
                                    var lines = content.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(o => o.Trim()).Where(o => o.Length > 0);

                                    int currentLevel = 0;

                                    foreach (var line in lines)
                                    {
                                        int newIndent = StartsWithHowMany(line, '>') + 1;

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
                            case "bullets-ordered":
                                {
                                    var lines = content.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(o => o.Trim()).Where(o => o.Length > 0);

                                    int currentLevel = 0;

                                    foreach (var line in lines)
                                    {
                                        int newIndent = StartsWithHowMany(line, '>') + 1;

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

                            case "alert":
                            case "alert-default":
                            case "alert-info":
                                {
                                    if (!String.IsNullOrEmpty(title)) content = $"<h1>{title}</h1>{content}";
                                    html.Append($"<div class=\"alert alert-info\">{content}.</div>");
                                }
                                break;
                            case "alert-danger":
                                {
                                    if (!String.IsNullOrEmpty(title)) content = $"<h1>{title}</h1>{content}";
                                    html.Append($"<div class=\"alert alert-danger\">{content}.</div>");
                                }
                                break;
                            case "alert-warning":
                                {
                                    if (!String.IsNullOrEmpty(title)) content = $"<h1>{title}</h1>{content}";
                                    html.Append($"<div class=\"alert alert-warning\">{content}.</div>");
                                }
                                break;
                            case "alert-success":
                                {
                                    if (!String.IsNullOrEmpty(title)) content = $"<h1>{title}</h1>{content}";
                                    html.Append($"<div class=\"alert alert-success\">{content}.</div>");
                                }
                                break;

                            case "jumbotron":
                                {
                                    if (!String.IsNullOrEmpty(title)) content = $"<h1>{title}</h1>{content}";
                                    html.Append($"<div class=\"jumbotron\">{content}</div>");
                                }
                                break;

                            case "panel":
                            case "panel-default":
                                {
                                    html.Append("<div class=\"panel panel-default\">");
                                    html.Append($"<div class=\"panel-heading\">{title}</div>");
                                    html.Append($"<div class=\"panel-body\">{content}</div></div>");
                                }
                                break;
                            case "panel-primary":
                                {
                                    html.Append("<div class=\"panel panel-primary\">");
                                    html.Append($"<div class=\"panel-heading\">{title}</div>");
                                    html.Append($"<div class=\"panel-body\">{content}</div></div>");
                                }
                                break;
                            case "panel-success":
                                {
                                    html.Append("<div class=\"panel panel-success\">");
                                    html.Append($"<div class=\"panel-heading\">{title}</div>");
                                    html.Append($"<div class=\"panel-body\">{content}</div></div>");
                                }
                                break;
                            case "panel-info":
                                {
                                    html.Append("<div class=\"panel panel-info\">");
                                    html.Append($"<div class=\"panel-heading\">{title}</div>");
                                    html.Append($"<div class=\"panel-body\">{content}</div></div>");
                                }
                                break;
                            case "panel-warning":
                                {
                                    html.Append("<div class=\"panel panel-warning\">");
                                    html.Append($"<div class=\"panel-heading\">{title}</div>");
                                    html.Append($"<div class=\"panel-body\">{content}</div></div>");
                                }
                                break;
                            case "panel-danger":
                                {
                                    html.Append("<div class=\"panel panel-danger\">");
                                    html.Append($"<div class=\"panel-heading\">{title}</div>");
                                    html.Append($"<div class=\"panel-body\">{content}</div></div>");
                                }
                                break;
                        }
                        StoreMatch(pageContent, match.Value, html.ToString(), allowNestedDecode);
                    }
                }
            }

            /*
            TransformSyntaxHighlighters("cpp", "cpp");
            TransformSyntaxHighlighters("csharp", "C#");
            TransformSyntaxHighlighters("sql", "sql");
            TransformSyntaxHighlighters("vbnet", "vbnet");
            TransformSyntaxHighlighters("xml", "xml");
            TransformSyntaxHighlighters("css", "css");
            TransformSyntaxHighlighters("java", "java");
            */
        }

        /*
        private void TransformSyntaxHighlighters(string tag, string brush)
        {
            Regex rgx = new Regex("\\[\\[" + tag + "\\]\\]([\\s\\S]*?)\\[\\[\\/" + tag + "\\]\\]", RegexOptions.IgnoreCase);
            var matches = OrderMatchesByLengthDescending(rgx.Matches(pageContent.ToString()));
            foreach (var match in matches)
            {
                string rawValue = match.Value.Substring(tag.Length + 4, match.Value.Length - ((tag.Length * 2) + 9));
                rawValue = rawValue.Replace("<", "&lt;").Replace(">", "&gt;");
                StoreMatch(pageContent, match.Value, "<pre class='brush: " + brush + "; toolbar: false; auto-links: false;'>" + rawValue + "</pre>");
            }
        }
        */

        class OrderedMatch
        {
            public string Value { get; set; }
            public int Index { get; set; }
        }

        List<OrderedMatch> OrderMatchesByLengthDescending(MatchCollection matches)
        {
            var result = new List<OrderedMatch>();

            foreach (Match match in matches)
            {
                result.Add(new OrderedMatch
                {
                    Value = match.Value,
                    Index = match.Index
                });
            }

            return result.OrderByDescending(o => o.Value.Length).ToList();
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
            var matches = OrderMatchesByLengthDescending(rgx.Matches(pageContent.ToString()));

            foreach (var match in matches)
            {
                int equalSigns = 0;
                foreach (char c in match.Value)
                {
                    if (c != '=')
                    {
                        break;
                    }
                    equalSigns++;
                }
                if (equalSigns >= 2 && equalSigns <= 6)
                {
                    string tag = _tocName + "_" + _tocTags.Count().ToString();
                    string value = match.Value.Substring(equalSigns, match.Value.Length - equalSigns).Trim();

                    int fontSize = 8 - equalSigns;
                    if (fontSize < 5) fontSize = 5;

                    string link = "<font size=\"" + fontSize + "\"><a name=\"" + tag + "\"><span class=\"WikiH" + (equalSigns - 1).ToString() + "\">" + value + "</span></a></font>";
                    StoreMatch(pageContent, match.Value.Trim(), link);
                    _tocTags.Add(new TOCTag(equalSigns - 1, match.Index, tag, value));
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
            var matches = OrderMatchesByLengthDescending(rgx.Matches(pageContent.ToString()));
            foreach (var match in matches)
            {
                string keyword = match.Value.Substring(2, match.Value.Length - 4);
                int pipeIndex = keyword.IndexOf("|");
                if (pipeIndex > 0)
                {
                    string linkText = keyword.Substring(pipeIndex + 1);

                    if (linkText.StartsWith("src=", StringComparison.CurrentCultureIgnoreCase))
                    {
                        linkText = $"<img {linkText} border =\"0\" > ";
                    }

                    keyword = keyword.Substring(0, pipeIndex);

                    StoreMatch(pageContent, match.Value, "<a href=\"" + keyword + "\">" + linkText + "</a>");
                }
                else
                {
                    StoreMatch(pageContent, match.Value, "<a href=\"" + keyword + "\">" + keyword + "</a>");
                }
            }

            //Parse internal dynamic links. eg [[AboutUs|About Us]].
            rgx = new Regex(@"(\[\[.+?\]\])", RegexOptions.IgnoreCase);
            matches = OrderMatchesByLengthDescending(rgx.Matches(pageContent.ToString()));
            foreach (var match in matches)
            {
                string keyword = match.Value.Substring(2, match.Value.Length - 4);
                string explicitLinkText = "";
                string linkText;

                int pipeIndex = keyword.IndexOf("|");
                if (pipeIndex > 0)
                {
                    explicitLinkText = keyword.Substring(pipeIndex + 1);
                    keyword = keyword.Substring(0, pipeIndex);
                }

                string pageName = keyword;
                string pageNavigation = HTML.CleanPartialURI(pageName);
                var page = PageRepository.GetPageByNavigation(pageNavigation);

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
                                string navigation = HTML.CleanPartialURI(linkText.Substring(0, slashIndex));
                                linkText = linkText.Substring(slashIndex + 1);

                                int scaleIndex = linkText.IndexOf("|");
                                if (scaleIndex > 0)
                                {
                                    scale = linkText.Substring(scaleIndex + 1);
                                    linkText = linkText.Substring(0, scaleIndex);
                                }

                                string attachementLink = $"/Wiki/Png/{navigation}?Image={linkText}";
                                linkText = $"<img src=\"{attachementLink}&Scale={scale}\" border=\"0\" />";
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

                                string attachementLink = $"/Wiki/Png/{_page.Navigation}?Image={linkText}";
                                linkText = $"<img src=\"{attachementLink}&Scale={scale}\" border=\"0\" />";
                            }
                        }
                        //External site image:
                        else if (compareString.StartsWith("src="))
                        {
                            linkText = linkText.Substring(linkText.IndexOf("=") + 1);
                            linkText = $"<img src=\"{linkText}\" border=\"0\" />";
                        }
                    }

                    StoreMatch(pageContent, match.Value, "<a href=\"" + HTML.CleanFullURI($"/Wiki/Show/{pageNavigation}") + $"\">{linkText}</a>");
                }
                else if (_context?.CanCreatePage() == true)
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
                    StoreMatch(pageContent, match.Value, "<a href=\"" + HTML.CleanFullURI($"/Wiki/Edit/{pageNavigation}/") + $"?Name={pageName}\">{linkText}</a>");
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
                        StoreMatch(pageContent, match.Value, linkText);
                    }
                    else
                    {
                        StoreError(pageContent, match.Value, $"The page has no name for {keyword}");
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
            Regex rgx = new Regex(@"(\@\@\w+)", RegexOptions.IgnoreCase);
            var matches = OrderMatchesByLengthDescending(rgx.Matches(pageContent.ToString()));
            foreach (var match in matches)
            {
                string keyword = match.Value.Substring(2, match.Value.Length - 2).Trim();

                switch (keyword.ToLower())
                {
                    case "depreciate":
                        ProcessingInstructions.Add(WikiInstruction.Depreciate);
                        pageContent.Insert(0, "<div class=\"alert alert-danger\">This page has been depreciate and will be deleted.</div>");
                        StoreMatch(pageContent, match.Value, "");
                        break;
                    case "template":
                        ProcessingInstructions.Add(WikiInstruction.Template);
                        pageContent.Insert(0, "<div class=\"alert alert-info\">This page is a template and will not appear in indexes or glossaries.</div>");
                        StoreMatch(pageContent, match.Value, "");
                        break;
                    case "review":
                        ProcessingInstructions.Add(WikiInstruction.Review);
                        pageContent.Insert(0, "<div class=\"alert alert-warning\">This page has been flagged for review, its content may be inaccurate.</div>");
                        StoreMatch(pageContent, match.Value, "");
                        break;
                    case "include":
                        ProcessingInstructions.Add(WikiInstruction.Include);
                        pageContent.Insert(0, "<div class=\"alert alert-info\">This page is an include and will not appear in indexes or glossaries.</div>");
                        StoreMatch(pageContent, match.Value, "");
                        break;
                    case "draft":
                        ProcessingInstructions.Add(WikiInstruction.Draft);
                        pageContent.Insert(0, "<div class=\"alert alert-warning\">This page is a draft and may contain incorrect information and/or experimental styling.</div>");
                        StoreMatch(pageContent, match.Value, "");
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
            Regex rgx = new Regex(@"(\#\#[\w-]+\(\))|(\#\#[\w-]+\(.*?\))", RegexOptions.IgnoreCase);
            var matches = OrderMatchesByLengthDescending(rgx.Matches(pageContent.ToString()));

            foreach (var match in matches)
            {
                string keyword = string.Empty;
                List<string> args = new List<string>();

                MatchCollection rawargs = (new Regex(@"\(+?\)|\(.+?\)")).Matches(match.Value);
                if (rawargs.Count > 0)
                {
                    keyword = match.Value.Substring(2, match.Value.IndexOf('(') - 2).ToLower();

                    foreach (var rawarg in rawargs)
                    {
                        string rawArgTrimmed = rawarg.ToString().Substring(1, rawarg.ToString().Length - 2);
                        args.AddRange(rawArgTrimmed.ToString().Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries));
                    }
                }
                else
                {
                    keyword = match.Value.Substring(2, match.Value.Length - 2).ToLower(); ; //The match has no parameter.
                }

                switch (keyword)
                {
                    //------------------------------------------------------------------------------------------------------------------------------
                    //Includes a page by it's navigation link.
                    case "include": //(PageCategory\PageName)
                        {
                            if (args.Count != 1)
                            {
                                StoreError(pageContent, match.Value, $"invalid number of parameters passed to ##{keyword}");
                                break;
                            }

                            Page page = GetPageFromPathInfo(args[0]);
                            if (page != null)
                            {
                                var wikify = new Wikifier(_context, page);
                                StoreMatch(pageContent, match.Value, wikify.ProcessedBody);
                            }
                            else
                            {
                                //Remove wiki tags for pages which were not found or which we do not have permission to view.
                                StoreMatch(pageContent, match.Value, "");
                            }
                        }
                        break;
                    //Associates tags with a page. These are saved with the page and can also be displayed.
                    case "settags": //##SetTags(comma,seperated,list,of,tags)
                        {
                            if (args.Count == 0)
                            {
                                StoreError(pageContent, match.Value, $"invalid number of parameters passed to ##{keyword}");
                                break;
                            }

                            Tags.AddRange(args);
                            StoreMatch(pageContent, match.Value, "");
                        }
                        break;
                    //Displays an image that is attached to the page.
                    case "image": //##Image(Name, [optional:default=100]Scale, [optional:default=""]Alt-Text)
                        if (args != null && args.Count > 0)
                        {
                            if (args.Count < 1 || args.Count > 2)
                            {
                                StoreError(pageContent, match.Value, $"invalid number of parameters passed to ##{keyword}");
                                break;
                            }

                            string imageName = args[0];
                            string navigation = _page.Navigation;
                            if (imageName.Contains("/"))
                            {
                                //Allow loading attacehd images from other pages.
                                int slashIndex = imageName.IndexOf("/");
                                navigation = HTML.CleanPartialURI(imageName.Substring(0, slashIndex));
                                imageName = imageName.Substring(slashIndex + 1);
                            }

                            string scale = "100";
                            string alt = imageName;

                            if (args.Count > 1)
                            {
                                scale = args[1];
                            }
                            if (args.Count > 2)
                            {
                                alt = args[2];
                            }

                            string link = $"/Wiki/Png/{navigation}?Image={imageName}";
                            string image = $"<a href=\"{link}\" target=\"_blank\"><img src=\"{link}&Scale={scale}\" border=\"0\" alt=\"{alt}\" /></a>";

                            StoreMatch(pageContent, match.Value, image);
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    //Displays a list of files attached to the page.
                    case "files": //##Files()
                        {
                            if (args.Count != 0)
                            {
                                StoreError(pageContent, match.Value, $"invalid number of parameters passed to ##{keyword}");
                                break;
                            }

                            var files = PageFileRepository.GetPageFilesInfoByPageId(_page.Id);

                            var html = new StringBuilder();

                            if (files.Count() > 0)
                            {
                                html.Append("<ul>");
                                foreach (var file in files)
                                {
                                    html.Append($"<li><a href=\"/Wiki/Download/{file.Name}\">{file.Name} ({file.FriendlySize})</a>");
                                    html.Append("</li>");
                                }
                                html.Append("</ul>");
                            }

                            StoreMatch(pageContent, match.Value, html.ToString());
                        }
                        break;


                    //------------------------------------------------------------------------------------------------------------------------------
                    //Creates a list of pages that have been recently modified.
                    case "recentlymodified": //##RecentlyModified(TopCount)
                    case "recentlymodifiedfull": //##RecentlyModifiedFull(TopCount)
                        {
                            if (args.Count != 1)
                            {
                                StoreError(pageContent, match.Value, $"invalid number of parameters passed to ##{keyword}");
                                break;
                            }

                            if (!int.TryParse(args[0], out int takeCount))
                            {
                                continue;
                            }

                            var pages = PageRepository.GetTopRecentlyModifiedPages(takeCount);

                            //If we specified a Top Count parameter, then we want to show the most recent pages
                            //  which were added to the category - otherwise we show ALL pages in the category so
                            //  we order them simply by name.
                            if (args.Count == 1)
                            {
                                pages = pages.OrderBy(p => p.Name).ToList();
                            }

                            var html = new StringBuilder();

                            if (pages.Count() > 0)
                            {
                                html.Append("<ul>");
                                foreach (var page in pages)
                                {
                                    html.Append($"<li><a href=\"/Wiki/Show/{page.Navigation}\">{page.Name}</a>");

                                    if (keyword == "recentlymodifiedfull")
                                    {
                                        if (page.Description.Length > 0)
                                        {
                                            html.Append(" - " + page.Description);
                                        }
                                    }
                                    html.Append("</li>");
                                }
                                html.Append("</ul>");
                            }

                            StoreMatch(pageContent, match.Value, html.ToString());
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Creates a glossary of pages with the specified comma seperated tags.
                    case "tagglossary":
                    case "tagglossaryfull":
                        {
                            if (args.Count == 0)
                            {
                                StoreError(pageContent, match.Value, $"invalid number of parameters passed to ##{keyword}");
                                break;
                            }

                            string glossaryName = "glossary_" + (new Random()).Next(0, 1000000).ToString();
                            string[] categoryName = args[0].ToLower().Split('|');

                            var pages = PageTagRepository.GetPageInfoByTags(args).OrderBy(o => o.Name).ToList();
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
                                        html.Append("<li><a href=\"/Wiki/Show/" + page.Navigation + "\">" + page.Name + "</a>");

                                        if (keyword == "tagglossaryfull")
                                        {
                                            if (page.Description.Length > 0)
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

                            StoreMatch(pageContent, match.Value, html.ToString());
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Creates a glossary by searching page's body text for the specified comma seperated list of words.
                    case "textglossary":
                    case "textglossaryfull":
                        {
                            string glossaryName = "glossary_" + (new Random()).Next(0, 1000000).ToString();
                            string[] searchStrings = args[0].ToLower().Split('|');

                            var pages = PageTagRepository.GetPageInfoByTokens(args).OrderBy(o => o.Name).ToList();
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
                                        html.Append("<li><a href=\"/Wiki/Show/" + page.Navigation + "\">" + page.Name + "</a>");

                                        if (keyword == "textglossaryfull")
                                        {
                                            if (page.Description.Length > 0)
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

                            StoreMatch(pageContent, match.Value, html.ToString());
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Creates a list of pages by searching the page body for the specified text.
                    case "textlist":
                    case "textlistfull":
                        {
                            var pages = PageTagRepository.GetPageInfoByTokens(args).OrderBy(o => o.Name).ToList();
                            var html = new StringBuilder();

                            if (pages.Count() > 0)
                            {
                                html.Append("<ul>");

                                foreach (var page in pages)
                                {
                                    html.Append("<li><a href=\"/Wiki/Show/" + page.Navigation + "\">" + page.Name + "</a>");

                                    if (keyword == "textlistfull")
                                    {
                                        if (page.Description.Length > 0)
                                        {
                                            html.Append(" - " + page.Description);
                                        }
                                    }
                                    html.Append("</li>");
                                }

                                html.Append("</ul>");
                            }

                            StoreMatch(pageContent, match.Value, html.ToString());
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Displays a list of other related pages based on tags.
                    case "related-flat":
                    case "related-full":
                    case "related": //##related
                        {
                            var html = new StringBuilder();

                            var relatedPages = PageRepository.GetRelatedPages(_page.Id).OrderBy(o => o.Name);

                            if (keyword == "related")
                            {
                                html.Append("<ul>");
                                foreach (var page in relatedPages)
                                {
                                    html.Append($"<li><a href=\"/Wiki/Show/{page.Navigation}\">{page.Name}</a>");
                                }
                                html.Append("</ul>");
                            }
                            else if (keyword == "related-flat")
                            {
                                foreach (var page in relatedPages)
                                {
                                    if (html.Length > 0) html.Append(" | ");
                                    html.Append($"<a href=\"/Wiki/Show/{page.Navigation}\">{page.Name}</a>");
                                }
                            }
                            else if (keyword == "related-full")
                            {
                                html.Append("<ul>");
                                foreach (var page in relatedPages)
                                {
                                    html.Append($"<li><a href=\"/Wiki/Show/{page.Navigation}\">{page.Name}</a> - {page.Description}");
                                }
                                html.Append("</ul>");
                            }

                            StoreMatch(pageContent, match.Value, html.ToString());
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Displays the date and time that the current page was last modified.
                    case "lastmodified":
                        {
                            if (args.Count != 0)
                            {
                                StoreError(pageContent, match.Value, $"invalid number of parameters passed to ##{keyword}");
                                break;
                            }

                            DateTime lastModified = DateTime.MinValue;
                            lastModified = _page.ModifiedDate;
                            if (lastModified != DateTime.MinValue)
                            {
                                StoreMatch(pageContent, match.Value, lastModified.ToShortDateString());
                            }
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Displays the date and time that the current page was created.
                    case "created":
                        {
                            if (args.Count != 0)
                            {
                                StoreError(pageContent, match.Value, $"invalid number of parameters passed to ##{keyword}");
                                break;
                            }

                            DateTime createdDate = DateTime.MinValue;
                            createdDate = _page.CreatedDate;
                            if (createdDate != DateTime.MinValue)
                            {
                                StoreMatch(pageContent, match.Value, createdDate.ToShortDateString());
                            }
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Displays the name of the current page.
                    case "name":
                        {
                            if (args.Count != 0)
                            {
                                StoreError(pageContent, match.Value, $"invalid number of parameters passed to ##{keyword}");
                                break;
                            }
                            StoreMatch(pageContent, match.Value, _page.Name);
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    //Displays the name of the current page in title form.
                    case "title":
                        {
                            if (args.Count != 0)
                            {
                                StoreError(pageContent, match.Value, $"invalid number of parameters passed to ##{keyword}");
                                break;
                            }
                            StoreMatch(pageContent, match.Value, $"<h1>{_page.Name}</h1>");
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    //Inserts empty lines into the page.
                    case "br":
                    case "nl":
                    case "newline": //##NewLine([optional:default=1]count)
                        {
                            int count = 1;

                            if (args.Count > 1)
                            {
                                StoreError(pageContent, match.Value, $"invalid number of parameters passed to ##{keyword}");
                                break;
                            }

                            if (args.Count > 0)
                            {
                                count = int.Parse(args[0]);
                            }

                            for (int i = 0; i < count; i++)
                            {
                                StoreMatch(pageContent, match.Value, $"<br />");
                            }
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Displays the navigation text for the current page.
                    case "navigation":
                        {
                            string navigation = string.Empty;

                            navigation = _page.Navigation;

                            if (navigation != string.Empty)
                            {
                                StoreMatch(pageContent, match.Value, navigation);
                            }
                        }
                        break;
                        //------------------------------------------------------------------------------------------------------------------------------                
                }
            }
        }

        /// <summary>
        /// Transform post-process are functions that must be called after all other transformations. For example, we can't build a table-of-contents until we have parsed the entire page.
        /// </summary>
        private void TransformPostProcess(StringBuilder pageContent)
        {
            Regex rgx = new Regex(@"(\#\#[\w-]+\(\))|(\#\#[\w-]+\(.*?\))", RegexOptions.IgnoreCase);
            var matches = OrderMatchesByLengthDescending(rgx.Matches(pageContent.ToString()));

            foreach (var match in matches)
            {
                string keyword = string.Empty;
                var args = new List<string>(); ;

                MatchCollection rawargs = (new Regex(@"\(+?\)|\(.+?\)")).Matches(match.Value);
                if (rawargs.Count > 0)
                {
                    keyword = match.Value.Substring(2, match.Value.IndexOf('(') - 2).ToLower();

                    foreach (var rawarg in rawargs)
                    {
                        string rawArgTrimmed = rawarg.ToString().Substring(1, rawarg.ToString().Length - 2);
                        args.AddRange(rawArgTrimmed.ToString().Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries));
                    }
                }
                else
                {
                    keyword = match.Value.Substring(2, match.Value.Length - 2).ToLower(); ; //The match has no parameter.
                }

                switch (keyword)
                {
                    //Displays a tag link list.
                    case "tags-flat": //##tags-flat
                    case "tags": //##tags
                        {
                            var html = new StringBuilder();

                            if (keyword == "tags")
                            {
                                html.Append("<ul>");
                                foreach (var tag in Tags)
                                {
                                    html.Append($"<li><a href=\"/Wiki/Tag/{tag}\">{tag}</a>");
                                }
                                html.Append("</ul>");
                            }
                            else if (keyword == "tags-flat")
                            {
                                foreach (var tag in Tags)
                                {
                                    if (html.Length > 0) html.Append(" | ");
                                    html.Append($"<a href=\"/Wiki/Tag/{tag}\">{tag}</a>");
                                }
                            }

                            StoreMatch(pageContent, match.Value, html.ToString());
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

                            StoreMatch(pageContent, match.Value, html.ToString());
                        }

                        break;
                        //------------------------------------------------------------------------------------------------------------------------------                
                }
            }
        }

        #region Linq Getters.

        public Page GetPageFromPathInfo(string routeData)
        {
            routeData = HTML.CleanFullURI(routeData);
            routeData = routeData.Substring(1, routeData.Length - 2);

            var page = PageRepository.GetPageByNavigation(routeData);

            return page;
        }

        #endregion
    }
}
