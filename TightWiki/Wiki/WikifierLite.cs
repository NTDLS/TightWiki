using System.Text.RegularExpressions;
using TightWiki.Library;
using TightWiki.Repository;
using TightWiki.Wiki.Function;

namespace TightWiki.Wiki
{
    /// <summary>
    /// Tiny wifier (reduced feature-set) for things like comments and profile bios.
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

                var emoji = GlobalSettings.Emojis.FirstOrDefault(o => o.Shortcut == key);

                if (GlobalSettings.Emojis.Exists(o => o.Shortcut == key))
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
                var args = FunctionParser.ParseRawArgumentsAddParens(keyword);

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
                var args = FunctionParser.ParseRawArgumentsAddParens(keyword);

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

                var args = FunctionParser.ParseRawArgumentsAddParens(keyword);

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
            //This function excpects an argument array with up to three argumens:
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

                    string attachementLink = $"/File/Image/{navigation}/{NamespaceNavigation.CleanAndValidate(linkText)}";
                    linkText = $"<img src=\"{attachementLink}?Scale={scale}\" border=\"0\" />";
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
