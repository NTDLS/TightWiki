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

            for (int i = 0; i < 100; i++) //We don't want to process nested wiki forever.
            {
                bool processedMatches = false;
                var matchStore = new Dictionary<string, string>();

                var content = new WikiString(unprocessedText);
                TransformEmoji(content, matchStore);
                TransformLinks(content, matchStore);
                TransformMarkup(content, matchStore);

                foreach (var match in matchStore)
                {
                    processedMatches = true;
                    content.Replace(match.Key, match.Value);
                }

                if (!processedMatches)
                {
                    break;
                }

                unprocessedText = content.ToString();
            }

            return unprocessedText;
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
            var symbols = WikiUtility.GetApplicableSymbols(pageContent.Value);

            foreach (var symbol in symbols)
            {
                var sequence = new string(symbol, 2);
                var escapedSequence = Regex.Escape(sequence);

                var rgx = new Regex(@$"{escapedSequence}(.*?){escapedSequence}", RegexOptions.IgnoreCase);
                var orderedMatches = WikiUtility.OrderMatchesByLengthDescending(rgx.Matches(pageContent.ToString()));
                foreach (var match in orderedMatches)
                {
                    string body = match.Value.Substring(sequence.Length, match.Value.Length - sequence.Length * 2);

                    var markup = symbol switch
                    {
                        '~' => $"<strike>{body}</strike>",
                        '*' => $"<strong>{body}</strong>",
                        '_' => $"<u>{body}</u>",
                        '/' => $"<i>{body}</i>",
                        '!' => $"<mark>{body}</mark>",
                        _ => body,
                    };

                    pageContent.Replace(match.Value, StoreMatch(matchStore, markup));
                }
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
                var args = FunctionParser.ParseArgumentsAddParenthesis(keyword);

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
                var args = FunctionParser.ParseArgumentsAddParenthesis(keyword);

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
                var args = FunctionParser.ParseArgumentsAddParenthesis(keyword);

                if (args.Count == 1)
                {
                    pageContent.Replace(match.Value, StoreMatch(matchStore, $"<a href=\"{GlobalConfiguration.BasePath}/{args[0]}\">{args[0]}</a>"));
                }
                else if (args.Count > 1)
                {
                    pageContent.Replace(match.Value, StoreMatch(matchStore, $"<a href=\"{GlobalConfiguration.BasePath}/{args[0]}\">{args[1]}</a>"));
                }
            }
        }
    }
}
