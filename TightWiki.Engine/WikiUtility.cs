using System.Text;
using System.Text.RegularExpressions;
using TightWiki.Engine.Library;

namespace TightWiki.Engine
{
    internal static class WikiUtility
    {
        /// <summary>
        /// Skips the namespace and returns just the page name part of the navigation.
        /// </summary>
        /// <param name="navigation"></param>
        /// <returns></returns>
        internal static string GetPageNamePart(string navigation)
        {
            var parts = navigation.Trim(':').Trim().Split("::");
            if (parts.Length > 1)
            {
                return string.Join('_', parts.Skip(1));
            }
            return navigation.Trim(':');
        }

        internal static string WarningCard(string header, string exceptionText)
        {
            var html = new StringBuilder();
            html.Append("<div class=\"card bg-warning mb-3\">");
            html.Append($"<div class=\"card-header\"><strong>{header}</strong></div>");
            html.Append("<div class=\"card-body\">");
            html.Append($"<p class=\"card-text\">{exceptionText}");
            html.Append("</p>");
            html.Append("</div>");
            html.Append("</div>");
            return html.ToString();
        }

        internal static List<OrderedMatch> OrderMatchesByLengthDescending(MatchCollection matches)
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
        /// Gets a list of symbols where the symbol occurs consecutively, more than once. (e.g.  "##This##")
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        internal static HashSet<char> GetApplicableSymbols(string input)
        {
            var symbolCounts = new Dictionary<char, int>();
            char? previousChar = null;
            int consecutiveCount = 0;

            for (int i = 0; i < input.Length; i++)
            {
                char currentChar = input[i];

                if (char.IsLetterOrDigit(currentChar) || char.IsWhiteSpace(currentChar))
                {
                    continue;
                }

                if (previousChar.HasValue && currentChar == previousChar.Value)
                {
                    consecutiveCount++;

                    if (consecutiveCount > 1)
                    {
                        symbolCounts.TryGetValue(previousChar.Value, out int count);
                        symbolCounts[previousChar.Value] = count + 1;

                        consecutiveCount = 1;
                    }
                }
                else
                {
                    consecutiveCount = 1;
                }

                previousChar = currentChar;
            }

            return symbolCounts.Where(o => o.Value > 1).Select(o => o.Key).ToHashSet();
        }
    }
}
