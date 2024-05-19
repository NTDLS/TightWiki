using System;
using System.Linq;
using System.Text;

namespace TightWiki.Library
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

            str = str.Replace('\\', '/');
            str = str.Replace("&quot;", "\"");
            str = str.Replace("&amp;", "&");
            str = str.Replace("&lt;", "<");
            str = str.Replace("&gt;", ">");
            str = str.Replace("&nbsp;", " ");

            var sb = new StringBuilder();
            foreach (char c in str)
            {
                if (c == ' ' || c == '.')
                {
                    sb.Append("_");
                }
                else if ((c >= 'A' && c <= 'Z')
                    || (c >= 'a' && c <= 'z')
                    || (c >= '0' && c <= '9')
                    || c == '_' || c == '/'
                    || c == '-')
                {
                    sb.Append(c);
                }
            }

            string result = sb.ToString();
            string original;
            do
            {
                original = result;
                result = result.Replace("__", "_").Replace("-_", "_").Replace("_-", "_").Replace("\\", "/").Replace("//", "/");
            }
            while (result != original);

            return result.ToLower();
        }
    }
}
