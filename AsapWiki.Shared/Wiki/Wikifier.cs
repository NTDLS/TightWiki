using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System.IO;
using AsapWiki.Shared.Models;
using AsapWiki.Shared.Classes;

namespace AsapWiki.Shared.Wiki
{
    public class Wikifier
    {
        private Dictionary<string, string> _lookup;
        private StringBuilder _markup;
        private readonly string _tocName = "TOC_" + (new Random()).Next(0, 1000000).ToString();
        private readonly List<TOCTag> _tocTags = new List<TOCTag>();
        private readonly Page _page = null;
        private readonly StateContext _context;

        public Wikifier(StateContext context)
        {
            _context = context;
        }

        public string Transform(Page page)
        {
            _lookup = new Dictionary<string, string>();
            _markup = new StringBuilder(page.Body);

            TransformMarkup();
            TransformFunctions();
            TransformProcessingInstructions();
            TransformInnerLinks();
            TransformHTML();
            TransformPostProcess();
            TransformHashtags();
            TransformWhitespace();

            foreach (var v in _lookup)
            {
                _markup.Replace(v.Key, v.Value);
            }

            return _markup.ToString();
        }

        private int StoreMatch(string match, string value)
        {
            string identifier = "{" + Guid.NewGuid().ToString() + "}";
            _lookup.Add(identifier, value);

            int previousLength = _markup.Length;
            return (previousLength - _markup.Replace(match, identifier).Length);
        }

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

        private void TransformWhitespace()
        {
            _markup = _markup.Replace("\r\n", "\n");
            _markup = _markup.Replace("\n", "<br />");
        }

        private void TransformMarkup()
        {
            //Transform literal non-wiki strings.
            Regex rgx = new Regex(@"(\{\{\{.+?\}\}\})", RegexOptions.IgnoreCase);
            MatchCollection matches = rgx.Matches(_markup.ToString());
            foreach (Match match in matches)
            {
                string value = match.Value.Substring(3, match.Value.Length - 6);
                StoreMatch(match.Value, value);
            }

            TransformSyntaxHighlighters("cpp", "cpp");
            TransformSyntaxHighlighters("csharp", "C#");
            TransformSyntaxHighlighters("sql", "sql");
            TransformSyntaxHighlighters("vbnet", "vbnet");
            TransformSyntaxHighlighters("xml", "xml");
            TransformSyntaxHighlighters("css", "css");
            TransformSyntaxHighlighters("java", "java");

            TransformSectionHeadings();
        }

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

        void TransformSectionHeadings()
        {
            var regEx = new StringBuilder();
            regEx.Append(@"(\=\=\=\=\=\=.*)");
            regEx.Append(@"|");
            regEx.Append(@"(\=\=\=\=\=.*)");
            regEx.Append(@"|");
            regEx.Append(@"(\=\=\=\=.*)");
            regEx.Append(@"|");
            regEx.Append(@"(\=\=\=.*)");
            regEx.Append(@"|");
            regEx.Append(@"(\=\=.*)");

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

                    string link = "<font size=\"" + fontSize + "\"><a name=\"" + tag + "\"><span class=\"WikiH" + (equalSigns - 1).ToString() + "\">" + value + "</span></a></font>";
                    StoreMatch(match.Value, Regex.Replace(match.Value, match.ToString(), link, RegexOptions.IgnoreCase));
                    _tocTags.Add(new TOCTag(equalSigns - 1, match.Index, tag, value));
                }
            }
        }

        private void TransformHTML()
        {
            //Replace tables with border="1" with border="0". We do this because it is nearly impossible to edit
            //  pages in the wysiwyg editor when the tables have no border. So we keep them in the editor, but
            //  remove them before displaying them.
            Regex rgx = new Regex("(\\<table.+?border=\"1\".+?\\>)", RegexOptions.IgnoreCase);
            MatchCollection matches = rgx.Matches(_markup.ToString());
            foreach (Match match in matches)
            {
                StoreMatch(match.Value, Regex.Replace(match.Value, "border=\"1\"", "border=\"0\"", RegexOptions.IgnoreCase));
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
            Regex rgx = new Regex(@"(\#\#.*)", RegexOptions.IgnoreCase);
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
            Regex rgx = new Regex(@"(\#\#.+?\#\#)|(\#\#.*?\(.*?\)\#\#)", RegexOptions.IgnoreCase);
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
                        args.Add(rawarg.ToString().Substring(1, rawarg.ToString().Length - 2));
                    }
                }
                else
                {
                    keyword = match.Value.Substring(2, match.Value.Length - 4).ToLower(); ; //The match has no parameter.
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

                    //------------------------------------------------------------------------------------------------------------------------------
                    //Creates a list of pages in the specified category.
                    //  Optionally also only pulls n-number of pages ordered by decending by the last modified date (then by page name).
                    case "categorylist": //(CategoryName, [optional]TopCount)
                    case "categorylistfull": //(CategoryName, [optional]TopCount)
                        if (args != null && (args.Count == 1))
                        {
                            if (!int.TryParse(args[1], out int takeCount))
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
                                    html.Append($"<li><a href=\"/Page/View/{page.Navigation}\">{page.Name}</a>");

                                    if (keyword == "categorylistfull")
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
                        {
                            string name = string.Empty;

                            name = _page.Name;

                            if (name != string.Empty)
                            {
                                StoreMatch(match.Value, name);
                            }
                        }
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
            //Functions without parameters.
            Regex rgx = new Regex(@"(\#\#.*)", RegexOptions.IgnoreCase);
            MatchCollection matches = rgx.Matches(_markup.ToString());
            foreach (Match match in matches)
            {
                string keyword = match.Value.Substring(2, match.Value.Length - 2).Trim();

                switch (keyword.ToLower())
                {
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
