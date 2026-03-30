using System.Text;
using System.Text.RegularExpressions;
using TightWiki.Plugin.Engine;

namespace TightWiki.Engine
{
    internal static class TwWikiUtility
    {
        /// <summary>
        /// Skips the namespace and returns just the page name part of the navigation.
        /// </summary>
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

            html.AppendLine("<div class=\"card bg-warning mb-3\">");
            html.AppendLine($"  <div class=\"card-header\"><strong>{header}</strong></div>");
            html.AppendLine("  <div class=\"card-body\">");
            html.AppendLine($"    <p class=\"card-text mb-0\">{exceptionText}</p>");
            html.AppendLine("  </div>");
            html.AppendLine("</div>");

            return html.ToString();
        }

        internal static List<TwOrderedMatch> OrderMatchesByLengthDescending(MatchCollection matches)
        {
            var result = new List<TwOrderedMatch>();

            foreach (Match match in matches)
            {
                result.Add(new TwOrderedMatch
                {
                    Value = match.Value,
                    Index = match.Index
                });
            }

            return result.OrderByDescending(o => o.Value.Length).ToList();
        }

        internal static int FindNextConsecutive(bool isOpen, string input, int startIndex, out string? foundPattern)
        {
            var stringBuilder = new StringBuilder();

            for (int i = startIndex; i < input.Length; i++)
            {
                char currentChar = input[i];

                if (currentChar == '\r' || currentChar == '\n')
                {
                    foundPattern = null;
                    return i + 1;
                }

                if (char.IsLetterOrDigit(currentChar) || char.IsWhiteSpace(currentChar))
                {
                    continue;
                }

                //We want to skip patterns that are preceded or followed by whitespace, as they are less likely to be intentional consecutive symbols. (e.g. "This is a ##test##" vs "This is a ## test ##")
                if (isOpen)
                {
                    if (i < input.Length && char.IsWhiteSpace(input[i + 1]))
                    {
                        continue;
                    }
                }
                else
                {
                    if (i > 1 && char.IsWhiteSpace(input[i - 1]))
                    {
                        continue;
                    }
                }

                stringBuilder.Clear();
                for (; i < input.Length && input[i] == currentChar; i++)
                {
                    stringBuilder.Append(input[i]);
                }

                if (stringBuilder.Length >= 2)
                {
                    foundPattern = stringBuilder.ToString();
                    return i;
                }
            }

            foundPattern = null;
            return -1;
        }

        /// <summary>
        /// Gets a list of symbols where the symbol occurs consecutively, more than once. (e.g."##This##")
        /// </summary>
        internal static HashSet<char> GetApplicableSymbols(string input)
        {
            var applicableSymbols = new HashSet<char>();
            string? previousFoundPattern = null;
            int i = 0;

            while (i < input.Length)
            {
                int foundIndex = FindNextConsecutive(previousFoundPattern == null, input, i, out string? foundPattern);
                if (foundIndex < 0)
                {
                    break;
                }

                if (foundPattern != null && foundPattern == previousFoundPattern)
                {
                    applicableSymbols.Add(foundPattern[0]);
                    previousFoundPattern = null;
                }
                else
                {
                    previousFoundPattern = foundPattern;
                }

                i = foundIndex;
            }

            return applicableSymbols;
        }
    }
}
