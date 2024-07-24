using NTDLS.Helpers;
using System.Text.RegularExpressions;
using TightWiki.Configuration;
using TightWiki.Repository;
using TightWiki.Wiki.Function;

namespace TightWiki.Engine
{
    /// <summary>
    /// Tiny wikifier (reduced feature-set) for things like comments and profile bios.
    /// </summary>
    public class WikifierLite
    {
        public static string Process(string? unprocessedText)
        {
            unprocessedText = unprocessedText?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(unprocessedText))
            {
                return string.Empty;
            }

            var content = new WikiString(unprocessedText);
            TransformEmoji(content);
            TransformLinks(content);
            TransformMarkup(content);
            return content.ToString();
        }

        private static void TransformEmoji(WikiString pageContent)
        {
            var rgx = new Regex(@"(%%.+?%%)", RegexOptions.IgnoreCase);
            var matches = WikiUtility.OrderMatchesByLengthDescending(rgx.Matches(pageContent.ToString()));
            foreach (var match in matches)
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
                        pageContent.Replace(match.Value, emojiImage);
                    }
                    else
                    {
                        var emojiImage = $"<img src=\"/file/Emoji/{key.Trim('%')}\" alt=\"{emoji?.Name}\" />";
                        pageContent.Replace(match.Value, emojiImage);
                    }
                }
                else
                {
                    pageContent.Replace(match.Value, string.Empty);
                }
            }
        }

        /// <summary>
        /// Transform basic markup such as bold, italics, underline, etc. for single and multi-line.
        /// </summary>
        /// <param name="pageContent"></param>
        private static void TransformMarkup(WikiString pageContent)
        {
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
                    pageContent.Replace(match.Value, markup);
                }
            }
        }

        private static void ReplaceInlineHTMLMarker(WikiString pageContent, string mark, string htmlTag, bool escape)
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
                pageContent.Replace(match.Value, $"<{htmlTag}>{markup}</{htmlTag}>");
            }
        }

        /// <summary>
        /// Transform links, these can be internal Wiki links or external links.
        /// </summary>
        /// <param name="pageContent"></param>
        private static void TransformLinks(WikiString pageContent)
        {
            //Parse external explicit links. eg. [[http://test.net]].
            var rgx = new Regex(@"(\[\[http\:\/\/.+?\]\])", RegexOptions.IgnoreCase);
            var matches = WikiUtility.OrderMatchesByLengthDescending(rgx.Matches(pageContent.ToString()));
            foreach (var match in matches)
            {
                string keyword = match.Value.Substring(2, match.Value.Length - 4).Trim();
                var args = FunctionParser.ParseRawArgumentsAddParenthesis(keyword);

                if (args.Count > 1)
                {
                    string linkText = args[1];
                    if (linkText.StartsWith("src=", StringComparison.CurrentCultureIgnoreCase))
                    {
                        linkText = $"<img {linkText} border =\"0\" > ";
                    }

                    keyword = args[0];

                    pageContent.Replace(match.Value, "<a href=\"" + keyword + "\">" + linkText + "</a>");
                }
                else
                {
                    pageContent.Replace(match.Value, "<a href=\"" + keyword + "\">" + keyword + "</a>");
                }
            }

            //Parse external explicit links. eg. [[https://test.net]].
            rgx = new Regex(@"(\[\[https\:\/\/.+?\]\])", RegexOptions.IgnoreCase);
            matches = WikiUtility.OrderMatchesByLengthDescending(rgx.Matches(pageContent.ToString()));
            foreach (var match in matches)
            {
                string keyword = match.Value.Substring(2, match.Value.Length - 4).Trim();
                var args = FunctionParser.ParseRawArgumentsAddParenthesis(keyword);

                if (args.Count > 1)
                {
                    string linkText = args[1];
                    if (linkText.StartsWith("src=", StringComparison.CurrentCultureIgnoreCase))
                    {
                        linkText = $"<img {linkText} border =\"0\" > ";
                    }

                    keyword = args[0];

                    pageContent.Replace(match.Value, "<a href=\"" + keyword + "\">" + linkText + "</a>");
                }
                else
                {
                    pageContent.Replace(match.Value, "<a href=\"" + keyword + "\">" + keyword + "</a>");
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

                if (keyword.Contains("::"))
                {
                    explicitLinkText = keyword.Substring(keyword.IndexOf("::") + 2).Trim();
                    string ns = keyword.Substring(0, keyword.IndexOf("::")).Trim();

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

                if (page != null)
                {
                    if (explicitLinkText.Length > 0 && explicitLinkText.Contains("img="))
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

                    pageContent.Replace(match.Value, "<a href=\"" + WikiUtility.CleanFullURI($"/{pageNavigation}") + $"\">{linkText}</a>");
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
                        pageContent.Replace(match.Value, linkText);
                    }
                    else
                    {
                        pageContent.Replace(match.Value, $"The page has no name for [{keyword}]");
                    }
                }
            }
        }

        private static string GetLinkImage(List<string> arguments)
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

            string compareString = linkText.ToLower().RemoveWhitespace();

            //Internal page attached image:
            if (compareString.StartsWith("img="))
            {
                if (linkText.Contains("/"))
                {
                    linkText = linkText.Substring(linkText.IndexOf("=") + 1);
                    string scale = "100";

                    //Allow loading attached images from other pages.
                    int slashIndex = linkText.IndexOf("/");
                    string navigation = NamespaceNavigation.CleanAndValidate(linkText.Substring(0, slashIndex));
                    linkText = linkText.Substring(slashIndex + 1);

                    if (arguments.Count > 2)
                    {
                        scale = arguments[2];
                    }

                    string attachmentLink = $"/Page/Image/{navigation}/{NamespaceNavigation.CleanAndValidate(linkText)}";
                    linkText = $"<img src=\"{attachmentLink}?Scale={scale}\" border=\"0\" />";
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
    }
}
