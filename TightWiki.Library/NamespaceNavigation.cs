using System.Text;
using System.Text.RegularExpressions;

namespace TightWiki.Library
{
    public class NamespaceNavigation
    {
        private string _namespace = string.Empty;
        private string _page = string.Empty;
        private readonly bool _lowerCase = false;

        public string Namespace
        {
            get => _namespace;
            set => _namespace = CleanAndValidate(value.Replace("::", "_")).Trim();
        }

        public string Page
        {
            get => _page;
            set => _page = CleanAndValidate(value.Replace("::", "_")).Trim();
        }

        public string Canonical
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Namespace))
                {
                    return Page;
                }
                return $"{Namespace}::{Page}";
            }
            set
            {
                var cleanedAndValidatedValue = CleanAndValidate(value, _lowerCase);

                var parts = cleanedAndValidatedValue.Split("::");
                if (parts.Length < 2)
                {
                    Page = parts[0].Trim();
                }
                else
                {
                    Namespace = parts[0].Trim();
                    Page = string.Join("_", parts.Skip(1).Select(o => o.Trim())).Trim();
                }
            }
        }

        /// <summary>
        /// Creates a new instance of NamespaceNavigation.
        /// </summary>
        /// <param name="givenCanonical">Page navigation with optional namespace.</param>
        public NamespaceNavigation(string givenCanonical)
        {
            _lowerCase = true;
            Canonical = givenCanonical;
        }

        /// <summary>
        /// Creates a new instance of NamespaceNavigation.
        /// </summary>
        /// <param name="givenCanonical">Page navigation with optional namespace.</param>
        /// <param name="lowerCase">If false, the namespace and page name will not be lowercased.</param>
        public NamespaceNavigation(string givenCanonical, bool lowerCase)
        {
            _lowerCase = lowerCase;
            Canonical = givenCanonical;
        }

        public override string ToString()
        {
            return Canonical;
        }

        /// <summary>
        /// Takes a page name with optional namespace and returns the cleaned version that can be used for matching Navigations.
        /// </summary>
        /// <param name="givenCanonical">Page navigation with optional namespace.</param>
        /// <param name="lowerCase">If false, the namespace and page name will not be lowercased.</param>
        public static string CleanAndValidate(string? str, bool lowerCase = true)
        {
            if (str == null)
            {
                return string.Empty;
            }

            //Fix names like "::Page" or "Namespace::".
            str = str.Trim().Trim([':']).Trim();

            if (str.Contains("::"))
            {
                var parts = str.Split("::");
                if (parts.Length != 2)
                {
                    throw new Exception("Navigation can not contain more than one namespace.");
                }
                return $"{CleanAndValidate(parts[0].Trim())}::{CleanAndValidate(parts[1].Trim(), lowerCase)}";
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


            if (lowerCase)
            {
                return result.TrimEnd(['/', '\\']).ToLowerInvariant();
            }
            else
            {
                return result.TrimEnd(['/', '\\']);
            }
        }
    }
}
