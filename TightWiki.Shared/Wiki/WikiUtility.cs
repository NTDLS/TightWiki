using DuoVia.FuzzyStrings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TightWiki.Shared.Library;
using TightWiki.Shared.Models;
using TightWiki.Shared.Models.Data;
using TightWiki.Shared.Repository;

namespace TightWiki.Shared.Wiki
{
    public static class WikiUtility
    {
        public static Page GetPageFromPathInfo(string routeData)
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

        public static string BuildTagCloud(string seedTag)
        {
            var tags = PageTagRepository.GetAssociatedTags(seedTag).OrderByDescending(o => o.PageCount).ToList();

            int tagCount = tags.Count();
            int fontSize = 7;
            int sizeStep = (tagCount > fontSize ? tagCount : (fontSize * 2)) / fontSize;
            int tagIndex = 0;

            var tagList = new List<TagCoudItem>();

            foreach (var tag in tags)
            {
                tagList.Add(new TagCoudItem(tag.Tag, tagIndex, "<font size=\"" + fontSize + "\"><a href=\"/Tag/Browse/" + WikiUtility.CleanFullURI(tag.Tag) + "\">" + tag.Tag + "</a></font>"));

                if ((tagIndex % sizeStep) == 0)
                {
                    fontSize--;
                }

                tagIndex++;
            }

            var cloudHtml = new StringBuilder();

            tagList.Sort(TagCoudItem.CompareItem);

            cloudHtml.Append("<table align=\"center\" border=\"0\" width=\"100%\"><tr><td><p align=\"justify\">");

            foreach (TagCoudItem tag in tagList)
            {
                cloudHtml.Append(tag.HTML + "&nbsp; ");
            }

            cloudHtml.Append("</p></td></tr></table>");

            return cloudHtml.ToString();
        }

        public static string BuildSearchCloud(List<string> tokens)
        {
            var searchTerms = (from o in tokens
                               select new PageToken
                               {
                                   Token = o,
                                   DoubleMetaphone = o.ToDoubleMetaphone()
                               }).ToList();

            var pages = PageRepository.PageSearch(searchTerms).OrderByDescending(o => o.Score).ToList();

            int pageCount = pages.Count();
            int fontSize = 7;
            int sizeStep = (pageCount > fontSize ? pageCount : (fontSize * 2)) / fontSize;
            int pageIndex = 0;

            var pageList = new List<TagCoudItem>();

            foreach (var page in pages)
            {
                pageList.Add(new TagCoudItem(page.Name, pageIndex, "<font size=\"" + fontSize + "\"><a href=\"/" + page.Navigation + "\">" + page.Name + "</a></font>"));

                if ((pageIndex % sizeStep) == 0)
                {
                    fontSize--;
                }

                pageIndex++;
            }

            var cloudHtml = new StringBuilder();

            pageList.Sort(TagCoudItem.CompareItem);

            cloudHtml.Append("<table align=\"center\" border=\"0\" width=\"100%\"><tr><td><p align=\"justify\">");

            foreach (TagCoudItem tag in pageList)
            {
                cloudHtml.Append(tag.HTML + "&nbsp; ");
            }

            cloudHtml.Append("</p></td></tr></table>");

            return cloudHtml.ToString();
        }

        public static string CleanPartialURI(string url)
        {
            if (url == null) return null;

            var sb = new StringBuilder();

            url = url.Replace('\\', '/');
            url = url.Replace("&quot;", "\"");
            url = url.Replace("&amp;", "&");
            url = url.Replace("&lt;", "<");
            url = url.Replace("&gt;", ">");
            url = url.Replace("&nbsp;", " ");

            foreach (char c in url)
            {
                if (c == ' ' || c == '.')
                {
                    sb.Append("_");
                }
                else if ((c >= 'A' && c <= 'Z')
                    || (c >= 'a' && c <= 'z')
                    || (c >= '0' && c <= '9')
                    || c == '_' || c == '/'
                    || c == '-')
                {
                    sb.Append(c);
                }
            }

            string result = sb.ToString();
            string original;
            do
            {
                original = result;
                result = result.Replace("__", "_").Replace("\\", "/").Replace("//", "/");
            }
            while (result != original);

            return result;
        }

        public static string CleanFullURI(string url)
        {
            string result = CleanPartialURI(url);

            if (result[result.Length - 1] != '/')
            {
                result = result + "/";
            }

            return result.TrimEnd(new char[] { '/', '\\' });
        }

        public static List<WeightedToken> ParsePageTokens(string content, int weightMultiplier)
        {
            var searchConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Search");

            var exclusionWords = searchConfig.As<string>("Word Exclusions").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Distinct();
            var strippedContent = HTML.StripHtml(content).ToLower();
            var tokens = strippedContent.Split(new char[] { ' ', '\n', '\t' }).ToList<string>().ToList();

            if (searchConfig.As<bool>("Split Camel Case"))
            {
                var casedTokens = new List<string>();

                foreach (var token in tokens)
                {
                    var spkitTokens = WikiUtility.SplitCamelCase(token).Split(' ');
                    if (spkitTokens.Count() > 1)
                    {
                        casedTokens.AddRange(spkitTokens);
                    }
                }

                tokens.AddRange(casedTokens);
            }

            tokens.RemoveAll(o => exclusionWords.Contains(o));

            var searchTokens = (from w in tokens
                                group w by w into g
                                select new WeightedToken
                                {
                                    Token = g.Key,
                                    Weight = g.Count() * weightMultiplier
                                }).ToList();

            return searchTokens;
        }

        public static bool IsValidEmail(string email)
        {
            email = email.Trim();

            if (email.EndsWith("."))
            {
                return false;
            }
            try
            {
                return ((new System.Net.Mail.MailAddress(email))?.Address == email);
            }
            catch
            {
                return false;
            }
        }

        public static bool ParseBool(object value)
        {
            if (value != null)
            {
                if (int.TryParse(value.ToString(), out int boolValue))
                {
                    return (boolValue != 0);
                }
                switch (value.ToString().ToUpper())
                {
                    case "TRUE":
                    case "YES":
                        return true;
                }
            }
            return false;
        }

        public static string TitleCase(string value)
        {
            return System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(value.ToLower());
        }

        public static string SplitCamelCase(string text)
        {
            return Regex.Replace(Regex.Replace(Regex.Replace(text, @"(\P{Ll})(\P{Ll}\p{Ll})", "$1 $2"), @"(\p{Ll})(\P{Ll})", "$1 $2"), @"\s+", " ");
        }

        public static string GetFriendlySize(long size)
        {
            double s = size;

            string[] format = new string[] { "{0} bytes", "{0} KB", "{0} MB", "{0} GB", "{0} TB", "{0} PB", "{0} EB" };

            int i = 0;
            while (i < format.Length && s >= 1024)
            {
                s = (int)(100 * s / 1024) / 100.0;
                i++;
            }

            return string.Format(format[i], s);
        }
    }
}
