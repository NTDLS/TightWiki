using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System.IO;
using AsapWiki.Shared.Models;
using AsapWiki.Shared.Classes;
using System.Web;

namespace AsapWiki.Shared.Wiki
{
    public class Wikifier
    {
        private List<string> _tags = new List<string>();
        private Dictionary<string, string> _lookup;
        private StringBuilder _markup;
        private readonly string _tocName = "TOC_" + (new Random()).Next(0, 1000000).ToString();
        private readonly List<TOCTag> _tocTags = new List<TOCTag>();
        private Page _page;
        private readonly StateContext _context;

        public Wikifier(StateContext context)
        {
            _context = context;
        }

        public string Transform(Page page)
        {
            _page = page;

            _lookup = new Dictionary<string, string>();
            _markup = new StringBuilder(page.Body);

            TransformLiterals();
            TransformInnerLinks();
            TransformMarkup();
            TransformSectionHeadings();
            TransformFunctions();
            TransformProcessingInstructions();
            TransformPostProcess();
            //TransformHashtags();
            TransformWhitespace();

            //We have to replace a few times because we could have replace tags (guids) nested inside others.
            int length;
            do
            {
                length = _markup.Length;
                foreach (var v in _lookup)
                {
                    _markup.Replace(v.Key, v.Value);
                }
            } while (length != _markup.Length);

            return _markup.ToString();
        }

        private int StoreMatch(string match, string value)
        {
            string identifier = "{" + Guid.NewGuid().ToString() + "}";
            _lookup.Add(identifier, value);

            int previousLength = _markup.Length;
            return (previousLength - _markup.Replace(match, identifier).Length);
        }

        private void StoreMatch(int startPosition, int length, string value)
        {
            string identifier = "{" + Guid.NewGuid().ToString() + "}";
            _lookup.Add(identifier, value);

            _markup.Remove(startPosition, length);
            _markup.Insert(startPosition, identifier);
        }

        /*
        private void TransformHashtags()
        {
            //Remove hashtags, they are stored with the page but not displayed.
            Regex rgx = new Regex(@"(?:\s|^)#[A-Za-z0-9\-_\.]+", RegexOptions.IgnoreCase);
            MatchCollection matches = rgx.Matches(_markup.ToString());
            foreach (Match match in matches)
            {
                StoreMatch(match.Value, String.Empty);
            }
        }
        */

        private void TransformWhitespace()
        {
            _markup = _markup.Replace("\r\n", "\n");
            _markup = _markup.Replace("\n\n", "<br />");
        }

        /// <summary>
        /// Replaces HTML where we are transforming the entire line, such as "*this will be bold" - > "<b>this will be bold</b>
        /// </summary>
        /// <param name="regex"></param>
        /// <param name="htmlTag"></param>
        void ReplaceWholeLineHTMLMarker(string regex, string htmlTag)
        {
            Regex rgx = new Regex(regex, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            MatchCollection matches = rgx.Matches(_markup.ToString());
            //We roll-through these matches in reverse order because we are replacing by position. We don't move the earlier positions by replacing from the bottom up.
            for (int i = matches.Count - 1; i > -1; i--)
            {
                var match = matches[i];
                string value = match.Value.Substring(1, match.Value.Length - 1).Trim();
                var matxhString = match.Value.Trim(); //We trim the match because we are matching to the end of the line which includes the \r\n, which we do not want to replace.
                StoreMatch(match.Index, matxhString.Length, $"<{htmlTag}>{value}</{htmlTag}> ");
            }
        }

        void ReplaceInlineHTMLMarker(string mark, string htmlTag)
        {
            string marker = string.Empty;
            foreach (var c in mark)
            {
                marker += $"\\{c}";
            }

            Regex rgx = new Regex($@"{marker}.*?{marker}", RegexOptions.IgnoreCase);
            MatchCollection matches = rgx.Matches(_markup.ToString());
            foreach (Match match in matches)
            {
                string value = match.Value.Substring(mark.Length, match.Value.Length - (mark.Length * 2));

                StoreMatch(match.Value, value);
            }
        }

        private void TransformLiterals()
        {
            //Transform literal strings, even encodes HTML so that it displays verbatim.
            Regex rgx = new Regex(@"{{{([\S\s]*?)}}}", RegexOptions.IgnoreCase);
            MatchCollection matches = rgx.Matches(_markup.ToString());
            foreach (Match match in matches)
            {
                string value = match.Value.Substring(3, match.Value.Length - 6);
                value = HttpUtility.HtmlEncode(value);
                StoreMatch(match.Value, value);
            }

            //Transform literal non-wiki strings, but still allow HTML.
            rgx = new Regex(@"\[\[\[([\S\s]*?)\]\]\]", RegexOptions.IgnoreCase);
            matches = rgx.Matches(_markup.ToString());
            foreach (Match match in matches)
            {
                string value = match.Value.Substring(3, match.Value.Length - 6);
                StoreMatch(match.Value, value);
            }
        }
        private void TransformMarkup()
        {
            ReplaceWholeLineHTMLMarker(@"^\*.*?\n", "strong"); //Single line bold.
            ReplaceWholeLineHTMLMarker(@"^_.*?\n", "u"); //Single line underline.
            ReplaceWholeLineHTMLMarker(@"^\/.*?\n", "i"); //Single line italics.

            ReplaceInlineHTMLMarker("*", "strong"); //inline bold.

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
            MatchCollection matches = rgx.Matches(_markup.ToString());
            foreach (Match match in matches)
            {
                string rawValue = match.Value.Substring(tag.Length + 4, match.Value.Length - ((tag.Length * 2) + 9));
                rawValue = rawValue.Replace("<", "&lt;").Replace(">", "&gt;");
                StoreMatch(match.Value, "<pre class='brush: " + brush + "; toolbar: false; auto-links: false;'>" + rawValue + "</pre>");
            }
        }
        */

        void TransformSectionHeadings()
        {
            var regEx = new StringBuilder();
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
            MatchCollection matches = rgx.Matches(_markup.ToString());
            foreach (Match match in matches)
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

                    string link = "<font size=\"" + fontSize + "\"><a name=\"" + tag + "\"><span class=\"WikiH" + (equalSigns - 1).ToString() + "\">" + value + "</span></a></font><br />";
                    StoreMatch(match.Value.Trim(), link);
                    _tocTags.Add(new TOCTag(equalSigns - 1, match.Index, tag, value));
                }
            }
        }

        private void TransformInnerLinks()
        {
            //Parse external explicit links. eg. [[http://test.net]].
            Regex rgx = new Regex(@"(\[\[http\:\/\/.+?\]\])", RegexOptions.IgnoreCase);
            MatchCollection matches = rgx.Matches(_markup.ToString());
            foreach (Match match in matches)
            {
                string keyword = match.Value.Substring(2, match.Value.Length - 4);
                int pipeIndex = keyword.IndexOf("|");
                if (pipeIndex > 0)
                {
                    string linkText = keyword.Substring(pipeIndex + 1);

                    if (linkText.StartsWith("src=", StringComparison.CurrentCultureIgnoreCase))
                    {
                        string border = "";

                        if (linkText.IndexOf("border", StringComparison.CurrentCultureIgnoreCase) < 0)
                        {
                            border = " border=\"0\"";
                        }

                        linkText = "<img " + linkText + border + ">";
                    }

                    keyword = keyword.Substring(0, pipeIndex);


                    StoreMatch(match.Value, "<a href=\"" + keyword + "\">" + linkText + "</a>");
                }
                else
                {
                    StoreMatch(match.Value, "<a href=\"" + keyword + "\">" + keyword + "</a>");
                }
            }

            //Parse internal explicit links. eg. [[/Login.aspx|Login Now]].
            rgx = new Regex(@"(\[\[\/.+?\]\])", RegexOptions.IgnoreCase);
            matches = rgx.Matches(_markup.ToString());
            foreach (Match match in matches)
            {
                string keyword = match.Value.Substring(2, match.Value.Length - 4);
                string linkText = string.Empty;
                string fileSize = string.Empty;
                bool isLinkImage = false;

                int pipeIndex = keyword.IndexOf("|");
                if (pipeIndex > 0)
                {
                    linkText = keyword.Substring(pipeIndex + 1);

                    if (linkText.StartsWith("src=", StringComparison.CurrentCultureIgnoreCase))
                    {
                        isLinkImage = true;
                        string border = "";

                        if (linkText.IndexOf("border", StringComparison.CurrentCultureIgnoreCase) < 0)
                        {
                            border = " border=\"0\"";
                        }

                        linkText = "<img " + linkText + border + ">";
                    }

                    keyword = keyword.Substring(0, pipeIndex);
                }
                else
                {
                    int iSlashIndex = keyword.LastIndexOf("/");
                    if (iSlashIndex > 0)
                    {
                        linkText = keyword.Substring(iSlashIndex + 1);
                    }
                    else
                    {
                        linkText = keyword;
                    }
                }

                /* //This looks like we are linking to a file, not a page. wtf?
                if (!isLinkImage)
                {
                    try
                    {
                        string fullFilePath = _basePage.Server.MapPath(keyword);
                        if (fullFilePath != "" && File.Exists(fullFilePath))
                        {
                            FileInfo fileInfo = new FileInfo(fullFilePath);
                            fileSize = " <font color=\"#666666\">(" + Utility.GetFriendlySize(fileInfo.Length) + ")</font>";
                        }
                    }
                    catch { }
                }
                */

                StoreMatch(match.Value, "<a href=\"" + keyword + "\">" + linkText + "</a>" + fileSize);
            }

            //Parse internal dynamic links. eg [[AboutUs|About Us]].
            rgx = new Regex(@"(\[\[.+?\]\])", RegexOptions.IgnoreCase);
            matches = rgx.Matches(_markup.ToString());
            foreach (Match match in matches)
            {
                string keyword = match.Value.Substring(2, match.Value.Length - 4);
                string explicitLinkText = "";
                string linkText;

                int pipeIndex = keyword.IndexOf("|");
                if (pipeIndex > 0)
                {
                    explicitLinkText = keyword.Substring(pipeIndex + 1);
                    keyword = keyword.Substring(0, pipeIndex);

                    if (explicitLinkText.StartsWith("src=", StringComparison.CurrentCultureIgnoreCase))
                    {
                        string border = "";

                        if (explicitLinkText.IndexOf("border", StringComparison.CurrentCultureIgnoreCase) < 0)
                        {
                            border = " border=\"0\"";
                        }

                        explicitLinkText = "<img " + explicitLinkText + border + ">";
                    }
                }

                string pageName = keyword;
                string pageNavigation = HTML.CleanFullURI(pageName).Replace("/", "");

                var page = Repository.PageRepository.GetPageByNavigation(pageNavigation);

                if (page != null)
                {
                    if (explicitLinkText.Length == 0)
                    {
                        linkText = page.Name;
                    }
                    else
                    {
                        linkText = explicitLinkText;
                    }

                    StoreMatch(match.Value, "<a href=\"" + HTML.CleanFullURI($"/Wiki/Show/{pageNavigation}") + $"\">{linkText}</a>");
                }
                else if (_context.CanCreatePage())
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
                    StoreMatch(match.Value, "<a href=\"" + HTML.CleanFullURI($"/Wiki/Edit/{pageNavigation}/") + $"?Name={pageName}\">{linkText}</a>");
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
                        StoreMatch(match.Value, linkText);
                    }
                    else
                    {
                        StoreMatch(match.Value, "");
                    }
                }
            }
        }

        private void TransformProcessingInstructions()
        {
            Regex rgx = new Regex(@"(\#\#\w+)", RegexOptions.IgnoreCase);
            MatchCollection matches = rgx.Matches(_markup.ToString());
            foreach (Match match in matches)
            {
                string keyword = match.Value.Substring(2, match.Value.Length - 2).Trim();

                switch (keyword.ToLower())
                {
                    case "depreciate":
                        _markup.Insert(0, "<font color=\"#cc0000\" size=\"3\">This page has been depreciate and will be deleted.<br /></font>");
                        StoreMatch(match.Value, "");
                        break;
                    case "draft":
                        _markup.Insert(0, "<font color=\"#cc0000\" size=\"3\">This page is a draft and may contain incorrect information and/or experimental styling.<br /></font>");
                        StoreMatch(match.Value, "");
                        break;
                }
            }
        }

        private string RemoveParens(string text)
        {
            return text.Substring(1, text.Length - 2);
        }

        private void TransformFunctions()
        {
            Regex rgx = new Regex(@"(\#\#.*?\(.*?\))|(\#\#.+?\(\))|(\#\#\w+)", RegexOptions.IgnoreCase);
            MatchCollection matches = rgx.Matches(_markup.ToString());

            foreach (Match match in matches)
            {
                string keyword = string.Empty;
                List<string> args = null;

                MatchCollection rawargs = (new Regex(@"\(+?\)|\(.+?\)")).Matches(match.Value);
                if (rawargs.Count > 0)
                {
                    args = new List<string>();
                    keyword = match.Value.Substring(2, match.Value.IndexOf('(') - 2).ToLower();

                    foreach (var rawarg in rawargs)
                    {
                        string rawArgTrimmed = rawarg.ToString().Substring(1, rawarg.ToString().Length - 2);
                        args.AddRange(rawArgTrimmed.ToString().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                    }
                }
                else
                {
                    keyword = match.Value.Substring(2, match.Value.Length - 2).ToLower(); ; //The match has no parameter.
                }

                switch (keyword)
                {
                    //------------------------------------------------------------------------------------------------------------------------------
                    //Includes a page by it's category name and page name.
                    case "include": //(PageCategory\PageName)
                        if (args != null && args.Count == 1)
                        {
                            Page page = GetPageFromPathInfo(args[0]);
                            if (page != null)
                            {
                                var wikify = new Wikifier(_context);

                                StoreMatch(match.Value, wikify.Transform(page));
                            }
                            else
                            {
                                //Remove wiki tags for pages which were not found or which we do not have permission to view.
                                StoreMatch(match.Value, "");
                            }
                        }
                        break;
                    //Associates tags with a page. These are saved with the page and can also be displayed.
                    case "settags": //##SetTags(draft,new,test)
                        if (args != null && args.Count > 0)
                        {
                            _tags.AddRange(args);
                            StoreMatch(match.Value, "");
                        }
                        break;
                    case "image":
                        if (args != null && args.Count > 0)
                        {
                            string imageName = args[0];
                            string scale = "100";

                            if (args.Count > 1)
                            {
                                scale = args[1];
                            }

                            string link = $"/Wiki/Png/{_page.Navigation}?Image={imageName}";
                            string image = $"<a href=\"{link}\"><img src=\"{link}&Scale={scale}\" alt=\"{imageName}\" border=\"0\" target=\"_blank\" /></a>";

                            StoreMatch(match.Value, image);
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    //Creates a list of pages in the specified category.
                    //  Optionally also only pulls n-number of pages ordered by decending by the last modified date (then by page name).
                    case "recentlymodified": //##RecentlyModified(TopCount)
                    case "recentlymodifiedfull": //##RecentlyModifiedFull(TopCount)
                        if (args != null && (args.Count == 1))
                        {
                            if (!int.TryParse(args[0], out int takeCount))
                            {
                                continue;
                            }

                            var pages = Repository.PageRepository.GetTopRecentlyModifiedPages(takeCount);

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

                            StoreMatch(match.Value, html.ToString());
                        }
                        break;

                    /*
                    //------------------------------------------------------------------------------------------------------------------------------
                    //Creates a glossary of pages in the specified comma seperated category names.
                    case "categoryglossary": //(CategoryNames)
                    case "categoryglossaryfull": //(CategoryNames)
                        if (args != null && args.Count == 1)
                        {
                            string glossaryName = "glossary_" + (new Random()).Next(0, 1000000).ToString();
                            string[] categoryName = args[0].ToLower().Split(',');

                            List<Page> pages = new List<Page>();

                            foreach (var searchString in categoryName)
                            {
                                var search = Repository.PageRepository.GetPagesByCategoryNavigation(searchString);
                                pages.AddRange(search);
                            }

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
                                        html.Append("<li><a href=\"/Page/View/" + page.CategoryNavigation + "/" + page.Navigation + "\">" + page.Name + "</a>");

                                        if (keyword == "categoryglossaryfull")
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

                            StoreMatch(match.Value, html.ToString());
                        }
                        break;
                    */
                    /*
                    //------------------------------------------------------------------------------------------------------------------------------
                    //Creates a glossary by searching page's body text for the specified comma seperated list of words.
                    case "textglossary": //(PageSearchText)
                    case "textglossaryfull": //(PageSearchText)
                        if (args != null && args.Count == 1)
                        {
                            string glossaryName = "glossary_" + (new Random()).Next(0, 1000000).ToString();
                            string[] searchStrings = args[0].ToLower().Split(',');

                            List<Page> pages = new List<Page>();

                            foreach (var searchString in searchStrings)
                            {
                                var search = Repository.PageRepository.GetPagesByBodyText(searchString);
                                pages.AddRange(search);
                            }

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
                                        html.Append("<li><a href=\"/Page/View/" + page.CategoryNavigation + "/" + page.Navigation + "\">" + page.Name + "</a>");

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

                            StoreMatch(match.Value, html.ToString());
                        }
                        break;
                    */
                    /*
                    //------------------------------------------------------------------------------------------------------------------------------
                    //Creates a list of pages by searching the page body for the specified text.
                    //  Optionally also only pulls n-number of pages ordered by decending by the last modified date (then by page name).
                    case "textlist": //(PageSearchText, [optional]TopCount)
                    case "textlistfull": //(PageSearchText, [optional]TopCount)
                        if (args != null && (args.Count == 1 || args.Count == 2))
                        {
                            string searchString = args[0].ToLower();
                            int takeCount = 100000;

                            if (args.Count > 1)
                            {
                                if (!int.TryParse(args[1], out takeCount))
                                {
                                    continue;
                                }
                            }

                            var pages = Repository.PageRepository.GetPagesByBodyText(searchString).Take(takeCount);

                            //If we specified a Top Count parameter, then we want to show the most recent
                            //  modified pages otherwise we show ALL pages simply ordered by name.
                            if (args.Count > 1)
                            {
                                pages = pages.OrderByDescending(p => p.ModifiedDate).ThenBy(p => p.Name);
                            }
                            else
                            {
                                pages = pages.OrderBy(p => p.Name);
                            }

                            var html = new StringBuilder();

                            if (pages.Count() > 0)
                            {
                                html.Append("<ul>");

                                foreach (var page in pages)
                                {
                                    html.Append("<li><a href=\"/Page/View/" + page.CategoryNavigation + "/" + page.Navigation + "\">" + page.Name + "</a>");

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

                            StoreMatch(match.Value, html.ToString());
                        }
                        break;
                    /*
                    /*
                    //------------------------------------------------------------------------------------------------------------------------------
                    //Creates a list of the most recently modified pages, optionally only returning the top n-pages.
                    case "recentlymodifiedfull": //(TopCount, [Optional]CategoryName)
                    case "recentlymodified": //(TopCount, [Optional]CategoryName)
                                             //Creates a list of the most recently created pages, optionally only returning the top n-pages.
                    case "recentlycreatedfull": //(TopCount, [Optional]CategoryName)
                    case "recentlycreated": //(TopCount, [Optional]CategoryName)
                        if (args != null && (args.Count == 1 || args.Count == 2))
                        {
                            string[] categoryNames = null;
                            int takeCount = 0;
                            if (!int.TryParse(args[0], out takeCount))
                            {
                                continue;
                            }

                            if (args.Count > 1)
                            {
                                categoryNames = args[1].ToLower().Split(',');
                            }

                            List<Page> pages = null;

                            if (categoryNames == null)
                            {
                                pages = Repository.PageRepository.GetAllPage();
                            }
                            else
                            {
                                pages = new List<Page>();

                                foreach (string categoryName in categoryNames)
                                {
                                    pages.AddRange(Repository.PageRepository.GetTopRecentlyModifiedPagesByCategoryNavigation(takeCount, categoryName));
                                }
                            }

                            if (keyword.ToLower().StartsWith("recentlymodified"))
                            {
                                pages = pages.OrderByDescending(p => p.ModifiedDate).ThenBy(p => p.Name).Take(takeCount).ToList();
                            }
                            else if (keyword.ToLower().StartsWith("recentlycreated"))
                            {
                                pages = pages.OrderByDescending(p => p.CreatedDate).ThenBy(p => p.Name).Take(takeCount).ToList();
                            }

                            var html = new StringBuilder();

                            if (pages.Count() > 0)
                            {
                                html.Append("<table cellpadding=\"1\" cellspacing=\"0\" border=\"0\" width=\"100%\">");

                                foreach (var page in pages)
                                {
                                    string date = string.Empty;

                                    if (keyword.ToLower().StartsWith("recentlymodified"))
                                    {
                                        date = page.ModifiedDate.ToString("MM/dd/yyyy");
                                    }
                                    else if (keyword.ToLower().StartsWith("recentlycreated"))
                                    {
                                        date = page.CreatedDate.ToString("MM/dd/yyyy");
                                    }

                                    html.Append("<tr>");
                                    html.Append("<td class=\"WikiModTableHead\" valign=\"top\" width=\"100%\">");
                                    html.Append("<span class=\"WikiModSpanDate\">" + date.ToString() + "</span>");

                                    html.Append("&nbsp;<a href=\"/Page/View/" + page.CategoryNavigation + "/" + page.Navigation + "\">" + page.Name + "</a>");

                                    html.Append("</td>");
                                    html.Append("</tr>");

                                    html.Append("<tr>");

                                    if (keyword.ToLower().EndsWith("full"))
                                    {
                                        html.Append("<tr>");
                                        html.Append("<td class=\"WikiModTableDetail\" valign=\"top\">");
                                        if (page.Description.Length > 0)
                                        {
                                            html.Append(page.Description);
                                        }
                                        html.Append("</td>");
                                        html.Append("</tr>");

                                        html.Append("<tr>");
                                        html.Append("<td colspan=\"2\" valign=\"top\">");
                                        html.Append("<img src=\"/Images/Site/Spacer.gif\" height=\"1\" width=\"1\" />");
                                        html.Append("</td>");
                                        html.Append("</tr>");
                                    }
                                }

                                html.Append("</table>");
                            }

                            StoreMatch(match.Value, html.ToString());
                        }
                        break;
                    */
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "lastmodified":
                        {
                            DateTime lastModified = DateTime.MinValue;

                            lastModified = _page.ModifiedDate;

                            if (lastModified != DateTime.MinValue)
                            {
                                StoreMatch(match.Value, lastModified.ToShortDateString());
                            }
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    case "created":
                        {
                            DateTime createdDate = DateTime.MinValue;

                            createdDate = _page.CreatedDate;

                            if (createdDate != DateTime.MinValue)
                            {
                                StoreMatch(match.Value, createdDate.ToShortDateString());
                            }
                        }
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    case "name":
                        StoreMatch(match.Value, _page.Name);
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
                    case "title":
                        StoreMatch(match.Value, $"<h1>{_page.Name}</h1>");
                        break;
                    case "br": //Line break;
                    case "nl": //New line.
                    case "newline": //New line
                        StoreMatch(match.Value, $"<br />");
                        break;

                    //------------------------------------------------------------------------------------------------------------------------------
                    case "navigation":
                        {
                            string navigation = string.Empty;

                            navigation = _page.Navigation;

                            if (navigation != string.Empty)
                            {
                                StoreMatch(match.Value, navigation);
                            }
                        }
                        break;
                        //------------------------------------------------------------------------------------------------------------------------------                
                }
            }
        }

        private void TransformPostProcess()
        {
            Regex rgx = new Regex(@"(\#\#.*?\(.*?\))|(\#\#.+?\(\))|(\#\#\w+)", RegexOptions.IgnoreCase);
            MatchCollection matches = rgx.Matches(_markup.ToString());

            foreach (Match match in matches)
            {
                string keyword = string.Empty;
                var args = new List<string>();                ;

                MatchCollection rawargs = (new Regex(@"\(+?\)|\(.+?\)")).Matches(match.Value);
                if (rawargs.Count > 0)
                {
                    keyword = match.Value.Substring(2, match.Value.IndexOf('(') - 2).ToLower();

                    foreach (var rawarg in rawargs)
                    {
                        string rawArgTrimmed = rawarg.ToString().Substring(1, rawarg.ToString().Length - 2);
                        args.AddRange(rawArgTrimmed.ToString().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                    }
                }
                else
                {
                    keyword = match.Value.Substring(2, match.Value.Length - 2).ToLower(); ; //The match has no parameter.
                }

                switch (keyword)
                {
                    //Displays a tag link list.
                    case "tags": //##tags([optional]Format=OrderedList|FlatList,[optional]Layout=Links|Text)
                        {
                            string format = "orderedlist";
                            string display = "links";

                            if (args.Count > 0) format = args[0].ToLower();
                            if (args.Count > 1) display = args[1].ToLower();

                            var html = new StringBuilder();

                            if (format == "orderedlist")
                            {
                                html.Append("<ul>");
                                foreach (var tag in _tags)
                                {
                                    if (display == "links")
                                    {
                                        html.Append($"<li><a href=\"/Wiki/Tag/{tag}\">{tag}</a>");
                                    }
                                    else if (display == "text")
                                    {
                                        html.Append($"<li>{tag}");
                                    }
                                }
                                html.Append("</ul>");
                            }
                            else if (format == "flatlist")
                            {
                                foreach (var tag in _tags)
                                {
                                    if (display == "links")
                                    {
                                        html.Append($"<a href=\"/Wiki/Tag/{tag}\">{tag}</a> ");
                                    }
                                    else if (display == "text")
                                    {
                                        html.Append($"{tag} ");
                                    }
                                }
                            }

                            StoreMatch(match.Value, html.ToString());
                        }
                        break;
                    //------------------------------------------------------------------------------------------------------------------------------
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

                            StoreMatch(match.Value, html.ToString());
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

            var page = Repository.PageRepository.GetPageByNavigation(routeData);

            return page;
        }

        #endregion
    }
}
