﻿using AsapWiki.Shared.Classes;
using AsapWiki.Shared.Models;
using AsapWiki.Shared.Repository;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AsapWiki.Shared.Wiki
{
    public static class Utility
    {
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
                tagList.Add(new TagCoudItem(tag.Tag, tagIndex, "<font size=\"" + fontSize + "\"><a href=\"/Tag/Browse/" + Utility.CleanFullURI(tag.Tag) + "\">" + tag.Tag + "</a></font>"));

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

        
        public static ParamSet GetNamedParams(List<string> args)
        {
            var set = new ParamSet();

            foreach (var arg in args)
            {
                if (arg.StartsWith(":") && arg.Contains("="))
                {
                    var parsed = arg.Substring(1); //Skip the colon.

                    int index = parsed.IndexOf("=");

                    var name = parsed.Substring(0, index).Trim().ToLower();
                    var value = parsed.Substring(index + 1).Trim();

                    set.Named.Add( new NamedParam(  name, value));
                }
                else
                {
                    set.Ordinals.Add(arg);
                }
            }

            return set;
        }

        public static string BuildSearchCloud(List<string> tokens)
        {
            var pages = PageTagRepository.GetPageInfoByTokens(tokens).OrderByDescending(o => o.TokenWeight).ToList();

            int pageCount = pages.Count();
            int fontSize = 7;
            int sizeStep = (pageCount > fontSize ? pageCount : (fontSize * 2)) / fontSize;
            int pageIndex = 0;

            var pageList = new List<TagCoudItem>();

            foreach (var page in pages)
            {
                pageList.Add(new TagCoudItem(page.Name, pageIndex, "<font size=\"" + fontSize + "\"><a href=\"/Tag/Browse/" + page.Navigation + "\">" + page.Name + "</a></font>"));

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
            var sb = new StringBuilder();

            url = url.Replace('\\', '/');
            url = url.Replace("&quot;", "\"");
            url = url.Replace("&amp;", "&");
            url = url.Replace("&lt;", "<");
            url = url.Replace("&gt;", ">");
            url = url.Replace("&nbsp;", " ");

            foreach (char c in url)
            {
                if (c == ' ')
                {
                    sb.Append("_");
                }
                else if ((c >= 'A' && c <= 'Z')
                    || (c >= 'a' && c <= 'z')
                    || (c >= '0' && c <= '9')
                    || c == '_' || c == '/'
                    || c == '.' || c == '-')
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
        public static List<WeightedToken> ParsePageTokens(string contentBody)
        {
            var exclusionWords = ConfigurationEntryRepository.GetConfigurationEntryValuesByGroupNameAndEntryName("Search", "Word Exclusions")
                .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Distinct();

            var htmlFree = HTML.StripHtml(contentBody).ToLower();
            var tokens = htmlFree.Split(new char[] { ' ', '\n', '\t' }).ToList<string>().ToList();
            var casedTokens = new List<string>();

            foreach (var token in tokens)
            {
                var spkitTokens = Utility.SplitCamelCase(token).Split(' ');
                if (spkitTokens.Count() > 1)
                {
                    casedTokens.AddRange(spkitTokens);
                }
            }

            tokens.AddRange(casedTokens);

            tokens.RemoveAll(o => exclusionWords.Contains(o));

            var searchTokens = (from w in tokens
                                group w by w into g
                                select new WeightedToken
                                {
                                    Token = g.Key,
                                    Weight = g.Count()
                                }).ToList();

            return searchTokens;
        }
        public static bool IsValidEmail(string email)
        {
            var trimmedEmail = email.Trim();

            if (trimmedEmail.EndsWith("."))
            {
                return false;
            }
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == trimmedEmail;
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
