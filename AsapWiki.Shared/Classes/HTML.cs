using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AsapWiki.Shared.Classes
{
    public static class HTML
    {
        public static string StripHTML(string input)
        {
            return Regex.Replace(input ?? "", "<.*?>", String.Empty);
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
    }
}
