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
    }
}
