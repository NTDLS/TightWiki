﻿using System.Text.RegularExpressions;

namespace TightWiki.Library
{
    public static class HTML
    {
        public static string StripHTML(string input)
        {
            return Regex.Replace(input ?? "", "<.*?>", String.Empty);
        }

        public static string StripHtml(string html)
        {
            html = html.Replace("\'", ""); //Compress "don't" -> "dont"
            html = html.Replace("`", ""); //Compress "don't" -> "dont"
            html = html.Replace("’", ""); //Compress "don't" -> "dont"
            html = (new Regex("<(.|\n)+?>")).Replace(html, " "); //Remove all text between < and >
            html = (new Regex("\\[\\[(.|\n)+?\\]\\]")).Replace(html, " "); //Remove all text between [[ and ]]
            html = (new Regex("\\&(.|\n)+?\\;")).Replace(html, " "); //Remove all text between & and ;
            html = (new Regex("[^A-Za-z]")).Replace(html, " "); //Remove all non-alpha-numeric
            html = (new Regex(@"\s+")).Replace(html, " "); // compress all whitespace to one space.

            return html.Trim();
        }
    }
}
