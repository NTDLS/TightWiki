using System.Text.RegularExpressions;
using TightWiki.Engine.Function;
using TightWiki.Models;

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

            var matchStore = new Dictionary<string, string>();

            var content = new WikiString(unprocessedText);
            TransformEmoji(content, matchStore);
            TransformLinks(content, matchStore);
            TransformMarkup(content, matchStore);

            foreach (var match in matchStore)
            {
                content.Replace(match.Key, match.Value);
            }

            return content.ToString();
        }

        private static string StoreMatch(Dictionary<string, string> matchStore, string value)
        {
            var guid = Guid.NewGuid().ToString();
            matchStore.Add(guid, value);
            return guid;
        }

        private static void TransformEmoji(WikiString pageContent, Dictionary<string, string> matchStore)
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
                        pageContent.Replace(match.Value, StoreMatch(matchStore, emojiImage));
                    }
                    else
                    {
                        var emojiImage = $"<img src=\"/file/Emoji/{key.Trim('%')}\" alt=\"{emoji?.Name}\" />";
                        pageContent.Replace(match.Value, StoreMatch(matchStore, emojiImage));
                    }
                }
                else
                {
                    pageContent.Replace(match.Value, StoreMatch(matchStore, string.Empty));
                }
            }
        }

        /// <summary>
        /// Transform basic markup such as bold, italics, underline, etc. for single and multi-line.
        /// </summary>
        /// <param name="pageContent"></param>
        private static void TransformMarkup(WikiString pageContent, Dictionary<string, string> matchStore)
        {
            ReplaceInlineHTMLMarker(pageContent, matchStore, "~~", "strike", true); //inline bold.
            ReplaceInlineHTMLMarker(pageContent, matchStore, "**", "strong", true); //inline bold.
            ReplaceInlineHTMLMarker(pageContent, matchStore, "__", "u", false); //inline highlight.
            ReplaceInlineHTMLMarker(pageContent, matchStore, "//", "i", true); //inline highlight.
            ReplaceInlineHTMLMarker(pageContent, matchStore, "!!", "mark", true); //inline highlight.

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

                    string markup = $"<font size=\"{fontSize}\">{value}</font>\r\n";
                    pageContent.Replace(match.Value, StoreMatch(matchStore, markup));
                }
            }
        }

        private static void ReplaceInlineHTMLMarker(WikiString pageContent, Dictionary<string, string> matchStore, string mark, string htmlTag, bool escape)
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
                var finalMarkup = $"<{htmlTag}>{markup}</{htmlTag}>";
                pageContent.Replace(match.Value, StoreMatch(matchStore, finalMarkup));
            }
        }

        /// <summary>
        /// Transform links, these can be internal Wiki links or external links.
        /// </summary>
        /// <param name="pageContent"></param>
        private static void TransformLinks(WikiString pageContent, Dictionary<string, string> matchStore)
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
                    pageContent.Replace(match.Value, StoreMatch(matchStore, $"<a href=\"{args[0]}\">{args[1]}</a>"));
                }
                else
                {
                    pageContent.Replace(match.Value, StoreMatch(matchStore, $"<a href=\"{args[0]}\">{args[0]}</a>"));
                }
            }

            //Parse external explicit links. eg. [[https://test.net]].
            rgx = new Regex(@"(\[\[https\:\/\/.+?\]\])", RegexOptions.IgnoreCase);
            matches = WikiUtility.OrderMatchesByLengthDescending(rgx.Matches(pageContent.ToString()));
            foreach (var match in matches)
            {
                string keyword = match.Value.Substring(2, match.Value.Length - 4).Trim();
                var args = FunctionParser.ParseRawArgumentsAddParenthesis(keyword);

                if (args.Count == 1)
                {
                    pageContent.Replace(match.Value, StoreMatch(matchStore, $"<a href=\"{args[0]}\">{args[1]}</a>"));
                }
                else if (args.Count > 1)
                {
                    pageContent.Replace(match.Value, StoreMatch(matchStore, $"<a href=\"{args[0]}\">{args[0]}</a>"));
                }
            }

            //Parse internal dynamic links. eg [[AboutUs|About Us]].
            rgx = new Regex(@"(\[\[.+?\]\])", RegexOptions.IgnoreCase);
            matches = WikiUtility.OrderMatchesByLengthDescending(rgx.Matches(pageContent.ToString()));
            foreach (var match in matches)
            {
                string keyword = match.Value.Substring(2, match.Value.Length - 4);
                var args = FunctionParser.ParseRawArgumentsAddParenthesis(keyword);

                if (args.Count == 1)
                {
                    pageContent.Replace(match.Value, StoreMatch(matchStore, $"<a href=\"/{args[0]}\">{args[0]}</a>"));
                }
                else if (args.Count > 1)
                {
                    pageContent.Replace(match.Value, StoreMatch(matchStore, $"<a href=\"/{args[0]}\">{args[1]}</a>"));
                }
            }
        }
    }
}
