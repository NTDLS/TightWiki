using System.Text;
using System.Text.RegularExpressions;

namespace TightWiki
{
    public class NamespaceNavigation
    {
        private string _category = string.Empty;
        private string _page = string.Empty;

        public string Category
        {
            get => _category;
            set => _category = value.Replace("::", "_").Trim();
        }

        public string Page
        {
            get => _page;
            set => _page = value.Replace("::", "_").Trim();
        }

        public string Canonical
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Category))
                {
                    return Page;
                }
                return $"{Category}::{Page}";
            }
            set
            {
                var cleanedAndValidatedValue = CleanAndValidate(value);

                var parts = cleanedAndValidatedValue.Split("::");
                if (parts.Length < 2)
                {
                    Page = parts[0].Trim();
                }
                else
                {
                    Category = parts[0].Trim();
                    Page = string.Join("_", parts.Skip(1).Select(o => o.Trim())).Trim();
                }
            }
        }

        public NamespaceNavigation(string givenCanonical)
        {
            Canonical = givenCanonical;
        }

        public override string ToString()
        {
            return Canonical;
        }


        public static string CleanAndValidate(string? str)
        {
            if (str == null)
            {
                return string.Empty;
            }

            if (str.Contains("::"))
            {
                var parts = str.Split("::");
                if (parts.Length != 2)
                {
                    throw new Exception("URL can not contain more than one namespace.");
                }
                return $"{CleanAndValidate(parts[0].Trim())}::{CleanAndValidate(parts[1].Trim())}";
            }

            // Decode common HTML entities
            str = str.Replace("&quot;", "\"")
                     .Replace("&amp;", "&")
                     .Replace("&lt;", "<")
                     .Replace("&gt;", ">")
                     .Replace("&nbsp;", " ");

            // Normalize backslashes to forward slashes
            str = str.Replace('\\', '/');

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
