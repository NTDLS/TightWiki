using AsapWiki.Shared.Classes;
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
