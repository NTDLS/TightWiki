using System.Text;
using System.Text.RegularExpressions;

namespace TightWiki.Plugin
{
    /// <summary>
    /// Represents a navigation target consisting of a namespace and page name, providing parsing, normalization, and
    /// canonicalization for namespace-based page navigation.
    /// </summary>
    /// <remarks>This class is useful for scenarios where page names may be prefixed with namespaces and
    /// require consistent formatting or validation, such as in wiki-like systems. It provides properties to access or
    /// modify the namespace, page, and canonical form, and ensures that values are cleaned and validated according to
    /// defined rules. The class supports both lowercased and case-sensitive navigation names, depending on the
    /// constructor parameter.</remarks>
    public class TwNamespaceNavigation
    {
        private string _namespace = string.Empty;
        private string _page = string.Empty;
        private readonly bool _lowerCase = false;

        /// <summary>
        /// Namespace part of the given canonical.
        /// </summary>
        public string Namespace
        {
            get => _namespace;
            set => _namespace = CleanAndValidate(value.Replace("::", "_")).Trim();
        }

        /// <summary>
        /// The page name part of the given canonical.
        /// </summary>
        public string Page
        {
            get => _page;
            set => _page = CleanAndValidate(value.Replace("::", "_")).Trim();
        }

        /// <summary>
        /// The full web-friendly page::canonical_path of the given canonical.
        /// </summary>
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
        public TwNamespaceNavigation(string givenCanonical)
        {
            _lowerCase = true;
            Canonical = givenCanonical;
        }

        /// <summary>
        /// Creates a new instance of NamespaceNavigation.
        /// </summary>
        /// <param name="givenCanonical">Page navigation with optional namespace.</param>
        /// <param name="lowerCase">If false, the namespace and page name will not be lowercased.</param>
        public TwNamespaceNavigation(string givenCanonical, bool lowerCase)
        {
            _lowerCase = lowerCase;
            Canonical = givenCanonical;
        }

        /// <summary>
        /// The full web-friendly page::canonical_path of the given canonical.
        /// </summary>
        public override string ToString()
        {
            return Canonical;
        }

        /// <summary>
        /// Takes a page name with optional namespace and returns the cleaned version that can be used for matching Navigations.
        /// </summary>
        /// <param name="givenCanonical">Page navigation with optional namespace.</param>
        /// <param name="lowerCase">If false, the namespace and page name will not be lowercased.</param>
        public static string CleanAndValidate(string? givenCanonical, bool lowerCase = true)
        {
            if (givenCanonical == null)
            {
                return string.Empty;
            }

            //Fix names like "::Page" or "Namespace::".
            givenCanonical = givenCanonical.Trim().Trim([':']).Trim();

            if (givenCanonical.Contains("::"))
            {
                var parts = givenCanonical.Split("::");
                if (parts.Length != 2)
                {
                    throw new Exception("Navigation can not contain more than one namespace.");
                }
                return $"{CleanAndValidate(parts[0].Trim())}::{CleanAndValidate(parts[1].Trim(), lowerCase)}";
            }

            // Decode common HTML entities
            givenCanonical = givenCanonical.Replace("&quot;", "\"")
                     .Replace("&amp;", "&")
                     .Replace("&lt;", "<")
                     .Replace("&gt;", ">")
                     .Replace("&nbsp;", " ");

            // Normalize backslashes to forward slashes
            givenCanonical = givenCanonical.Replace('\\', '/');

            var sb = new StringBuilder();
            foreach (char c in givenCanonical)
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
