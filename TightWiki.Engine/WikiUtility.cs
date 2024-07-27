using System.Text;
using System.Text.RegularExpressions;
using TightWiki.Engine.Library;

namespace TightWiki.Engine
{
    internal static class WikiUtility
    {
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

        internal static int StartsWithHowMany(string value, char ch)
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


    }
}
