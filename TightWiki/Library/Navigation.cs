using System.Text;
using System.Text.RegularExpressions;

namespace TightWiki.Library
{
    public class Navigation
    {
        public static string Clean(string? str)
        {
            if (str == null)
            {
                return string.Empty;
            }

            // Decode common HTML entities
            str = str.Replace("&quot;", "\"")
                     .Replace("&amp;", "&")
                     .Replace("&lt;", "<")
                     .Replace("&gt;", ">")
                     .Replace("&nbsp;", " ");

            // Normalize backslashes to forward slashes
            str = str.Replace('\\', '/');

            // Replace special sequences
            str = str.Replace("::", "_").Trim();

            var sb = new StringBuilder();
            foreach (char c in str)
            {
                if (char.IsWhiteSpace(c) || c == '.')
                {
                    sb.Append('_');
                }
                else if (char.IsLetterOrDigit(c) || c == '_' || c == '/' || c == '-')
                {
                    sb.Append(c);
                }
            }

            string result = sb.ToString();

            // Remove multiple consecutive underscores or slashes
            result = Regex.Replace(result, @"[_]{2,}", "_");
            result = Regex.Replace(result, @"[/]{2,}", "/");

            return result.ToLower();
        }
    }
}
