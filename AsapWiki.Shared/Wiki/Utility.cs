using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace AsapWiki.Shared.Wiki
{
    public static class Utility
    {
        public static bool CBool(object value)
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
                        return true;
                    case "YES":
                        return true;

                }
            }
            return false;
        }

        public static string CalculateSHA1(string text)
        {
            byte[] buffer = Encoding.Unicode.GetBytes(text);
            SHA1CryptoServiceProvider cryptoTransformSHA1 = new SHA1CryptoServiceProvider();
            string hash = BitConverter.ToString(cryptoTransformSHA1.ComputeHash(buffer)).Replace("-", "");
            return hash;
        }

        public static string StripHtml(string html)
        {
            html = (new Regex("<(.|\n)+?>")).Replace(html, " "); //Remove all text between < and >
            html = (new Regex("\\[\\[(.|\n)+?\\]\\]")).Replace(html, " "); //Remove all text between [[ and ]]
            html = (new Regex("\\&(.|\n)+?\\;")).Replace(html, " "); //Remove all text between & and ;
            html = (new Regex("[^A-Za-z]")).Replace(html, " "); //Remove all non-alpha-numerics
            html = (new Regex(@"\s+")).Replace(html, " "); // compress all whitespace to one space.

            return html.Trim();
        }

        public static string RemoveHTML(string html)
        {
            html = (new Regex("<(.|\n)+?>")).Replace(html, " "); //Remove all text between < and >
            html = (new Regex("\\[\\[(.|\n)+?\\]\\]")).Replace(html, " "); //Remove all text between [[ and ]]
            html = (new Regex("\\&(.|\n)+?\\;")).Replace(html, " "); //Remove all text between & and ;
            html = (new Regex(@"\s+")).Replace(html, " "); // compress all whitespace to one space.
            return html.Trim();
        }

        public static string SplitCamelCase(string text)
        {
            return Regex.Replace(Regex.Replace(Regex.Replace(text, @"(\P{Ll})(\P{Ll}\p{Ll})", "$1 $2"), @"(\p{Ll})(\P{Ll})", "$1 $2"), @"\s+", " ");
        }

        public static String CleanPartialURI(string url)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

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
                    || c == '.')
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
