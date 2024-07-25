using NTDLS.Helpers;
using System.Text;
using System.Text.RegularExpressions;
using TightWiki.Engine.Types;
using TightWiki.Models;
using TightWiki.Models.DataModels;
using TightWiki.Repository;

namespace TightWiki.Engine
{
    internal static class WikiUtility
    {
        internal static readonly Dictionary<string, BGFGStyle> ForegroundStyles = new(StringComparer.OrdinalIgnoreCase)
        {
            { "primary", new BGFGStyle("text-primary", "") },
            { "secondary", new BGFGStyle("text-secondary", "") },
            { "success", new BGFGStyle("text-success", "") },
            { "danger", new BGFGStyle("text-danger", "") },
            { "warning", new BGFGStyle("text-warning", "") },
            { "info", new BGFGStyle("text-info", "") },
            { "light", new BGFGStyle("text-light", "") },
            { "dark", new BGFGStyle("text-dark", "") },
            { "muted", new BGFGStyle("text-muted", "") },
            { "white", new BGFGStyle("text-white", "bg-dark") }
        };

        internal static readonly Dictionary<string, BGFGStyle> BackgroundStyles = new(StringComparer.OrdinalIgnoreCase)
        {
            { "muted", new BGFGStyle("text-muted", "") },
            { "primary", new BGFGStyle("text-white", "bg-primary") },
            { "secondary", new BGFGStyle("text-white", "bg-secondary") },
            { "info", new BGFGStyle("text-white", "bg-info") },
            { "success", new BGFGStyle("text-white", "bg-success") },
            { "warning", new BGFGStyle("bg-warning", "") },
            { "danger", new BGFGStyle("text-white", "bg-danger") },
            { "light", new BGFGStyle("text-black", "bg-light") },
            { "dark", new BGFGStyle("text-white", "bg-dark") }
        };

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

        internal static BGFGStyle GetBackgroundStyle(string style)
        {
            if (BackgroundStyles.TryGetValue(style, out var html))
            {
                return html;
            }

            return new BGFGStyle();
        }

        internal static BGFGStyle GetForegroundStyle(string style)
        {
            if (ForegroundStyles.TryGetValue(style, out var html))
            {
                return html;
            }

            return new BGFGStyle();
        }

        internal static Page? GetPageFromPathInfo(string routeData)
        {
            routeData = NamespaceNavigation.CleanAndValidate(routeData);
            var page = PageRepository.GetPageRevisionByNavigation(routeData);
            return page;
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

        internal static List<WeightedToken> ParsePageTokens(string content, double weightMultiplier)
        {
            var searchConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Search");

            var exclusionWords = searchConfig?.Value<string>("Word Exclusions")?.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Distinct() ?? new List<string>();
            var strippedContent = Html.StripHtml(content);
            var tokens = strippedContent.Split([' ', '\n', '\t', '-', '_']).ToList<string>().ToList();

            if (searchConfig?.Value<bool>("Split Camel Case") == true)
            {
                var casedTokens = new List<string>();

                foreach (var token in tokens)
                {
                    var splitTokens = NTDLS.Helpers.Text.SeperateCamelCase(token).Split(' ');
                    if (splitTokens.Count() > 1)
                    {
                        foreach (var lowerToken in splitTokens)
                        {
                            casedTokens.Add(lowerToken.ToLower());
                        }
                    }
                }

                tokens.AddRange(casedTokens);
            }

            tokens = tokens.ConvertAll(d => d.ToLower());

            tokens.RemoveAll(o => exclusionWords.Contains(o));

            var searchTokens = (from w in tokens
                                group w by w into g
                                select new WeightedToken
                                {
                                    Token = g.Key,
                                    Weight = g.Count() * weightMultiplier
                                }).ToList();

            return searchTokens.Where(o => string.IsNullOrWhiteSpace(o.Token) == false).ToList();
        }
    }
}
