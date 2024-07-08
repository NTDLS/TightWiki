using System.Text;
using System.Text.RegularExpressions;
using TightWiki.Library;
using TightWiki.Models;
using TightWiki.Models.DataModels;
using TightWiki.Repository;

namespace TightWiki.Wiki
{
    public static class WikiUtility
    {
        static readonly Dictionary<string, BGFGStyle> ForegroundStyles = new(StringComparer.OrdinalIgnoreCase)
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

        static readonly Dictionary<string, BGFGStyle> BackgroundStyles = new(StringComparer.OrdinalIgnoreCase)
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

        public static string WarningCard(string header, string exceptionText)
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

        public static BGFGStyle GetBackgroundStyle(string style)
        {
            if (BackgroundStyles.TryGetValue(style, out var bgfgStyle))
            {
                return bgfgStyle;
            }

            return new BGFGStyle();
        }

        public static BGFGStyle GetForegroundStyle(string style)
        {
            if (ForegroundStyles.TryGetValue(style, out var bgfgStyle))
            {
                return bgfgStyle;
            }

            return new BGFGStyle();
        }

        public static Page? GetPageFromPathInfo(string routeData)
        {
            routeData = WikiUtility.CleanFullURI(routeData).Trim(new char[] { '\\', '/' });
            var page = PageRepository.GetPageRevisionByNavigation(routeData);
            return page;
        }

        public static int StartsWithHowMany(string value, char ch)
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

        public static List<OrderedMatch> OrderMatchesByLengthDescending(MatchCollection matches)
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

        public static string BuildTagCloud(string seedTag, int? maxCount)
        {
            var tags = PageRepository.GetAssociatedTags(seedTag).OrderByDescending(o => o.PageCount).ToList();

            if (maxCount > 0)
            {
                tags = tags.Take((int)maxCount).ToList();
            }

            int tagCount = tags.Count();
            int fontSize = 7;
            int sizeStep = (tagCount > fontSize ? tagCount : (fontSize * 2)) / fontSize;
            int tagIndex = 0;

            var tagList = new List<TagCloudItem>();

            foreach (var tag in tags)
            {
                tagList.Add(new TagCloudItem(tag.Tag, tagIndex, "<font size=\"" + fontSize + "\"><a href=\"/Tag/Browse/" + WikiUtility.CleanFullURI(tag.Tag) + "\">" + tag.Tag + "</a></font>"));

                if ((tagIndex % sizeStep) == 0)
                {
                    fontSize--;
                }

                tagIndex++;
            }

            var cloudHtml = new StringBuilder();

            tagList.Sort(TagCloudItem.CompareItem);

            cloudHtml.Append("<table align=\"center\" border=\"0\" width=\"100%\"><tr><td><p align=\"justify\">");

            foreach (TagCloudItem tag in tagList)
            {
                cloudHtml.Append(tag.HTML + "&nbsp; ");
            }

            cloudHtml.Append("</p></td></tr></table>");

            return cloudHtml.ToString();
        }

        public static string BuildSearchCloud(List<string> searchTokens, int? maxCount = null)
        {
            var pages = PageRepository.PageSearch(searchTokens).OrderByDescending(o => o.Score).ToList();

            if (maxCount > 0)
            {
                pages = pages.Take((int)maxCount).ToList();
            }

            int pageCount = pages.Count();
            int fontSize = 7;
            int sizeStep = (pageCount > fontSize ? pageCount : (fontSize * 2)) / fontSize;
            int pageIndex = 0;

            var pageList = new List<TagCloudItem>();

            foreach (var page in pages)
            {
                pageList.Add(new TagCloudItem(page.Name, pageIndex, "<font size=\"" + fontSize + "\"><a href=\"/" + page.Navigation + "\">" + page.Name + "</a></font>"));

                if ((pageIndex % sizeStep) == 0)
                {
                    fontSize--;
                }

                pageIndex++;
            }

            var cloudHtml = new StringBuilder();

            pageList.Sort(TagCloudItem.CompareItem);

            cloudHtml.Append("<table align=\"center\" border=\"0\" width=\"100%\"><tr><td><p align=\"justify\">");

            foreach (TagCloudItem tag in pageList)
            {
                cloudHtml.Append(tag.HTML + "&nbsp; ");
            }

            cloudHtml.Append("</p></td></tr></table>");

            return cloudHtml.ToString();
        }

        public static string CleanFullURI(string url)
        {
            string result = NamespaceNavigation.CleanAndValidate(url);

            if (result[result.Length - 1] != '/')
            {
                result += "/";
            }

            return result.TrimEnd(['/', '\\']);
        }

        public static List<WeightedToken> ParsePageTokens(string content, double weightMultiplier)
        {
            var searchConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Search");

            var exclusionWords = searchConfig?.Value<string>("Word Exclusions")?.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Distinct() ?? new List<string>();
            var strippedContent = HTML.StripHtml(content);
            var tokens = strippedContent.Split([' ', '\n', '\t', '-', '_']).ToList<string>().ToList();

            if (searchConfig?.Value<bool>("Split Camel Case") == true)
            {
                var casedTokens = new List<string>();

                foreach (var token in tokens)
                {
                    var splitTokens = SeperateCamelCase(token).Split(' ');
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

        public static string SeperateCamelCase(string text)
        {
            return Regex.Replace(
                        Regex.Replace(
                            Regex.Replace(
                                text,
                                @"([a-z])([A-Z])", // Lowercase followed by uppercase
                                "$1 $2"
                            ),
                            @"([A-Z])([A-Z][a-z])", // Uppercase followed by uppercase and lowercase
                            "$1 $2"
                        ),
                        @"\s+",
                        " "
                    );
        }

        public static string GetFriendlySize(long size)
        {
            double s = size;
            string[] format = ["{0} bytes", "{0} KB", "{0} MB", "{0} GB", "{0} TB", "{0} PB", "{0} EB"];

            int i = 0;
            while (i < format.Length && s >= 1024)
            {
                s = (int)(100 * s / 1024) / 100.0;
                i++;
            }

            return string.Format(format[i], s);
        }

        public static string ReplaceFirst(string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }
    }
}
