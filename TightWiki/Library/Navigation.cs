using System.Text;

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

            str = str.Replace("::", "_").Trim();
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
