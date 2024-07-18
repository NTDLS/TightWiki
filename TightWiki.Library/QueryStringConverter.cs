using Microsoft.AspNetCore.Http;
using System.Text;
using System.Web;

namespace TightWiki.Library
{
    public static class QueryStringConverter
    {
        public static Dictionary<string, string> ToDictionary(IQueryCollection? queryString)
        {
            if (queryString == null)
            {
                return new Dictionary<string, string>();
            }

            var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in queryString)
            {
                //Technically, keys can be duplicated in a IQueryCollection but we do not
                //support this. Use .Single() to throw exception if duplicates are found.
                dictionary.Add(item.Key, item.Value.Single() ?? string.Empty);
            }

            return dictionary;
        }

        public static Dictionary<string, string> ToDictionary(QueryString? queryString)
            => ToDictionary(queryString?.ToString());

        public static Dictionary<string, string> ToDictionary(string? queryString)
        {
            var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(queryString))
            {
                return dictionary;
            }

            // If the query string starts with '?', remove it
            if (queryString.StartsWith('?'))
            {
                queryString = queryString.Substring(1);
            }

            // Split the query string into key-value pairs
            var keyValuePairs = queryString.Split('&');

            foreach (var kvp in keyValuePairs)
            {
                var keyValue = kvp.Split('=');
                if (keyValue.Length == 2)
                {
                    var key = HttpUtility.UrlDecode(keyValue[0]);
                    var value = HttpUtility.UrlDecode(keyValue[1]);
                    dictionary[key] = value;
                }
            }

            return dictionary;
        }

        public static string FromCollection(IQueryCollection? collection)
        {
            if (collection == null || collection.Count == 0)
            {
                return string.Empty;
            }

            var queryString = new StringBuilder();

            foreach (var kvp in collection)
            {
                if (queryString.Length > 0)
                {
                    queryString.Append('&');
                }

                queryString.Append($"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value.ToString())}");
            }

            return queryString.ToString();
        }

        public static string FromCollection(Dictionary<string, string>? collection)
        {
            if (collection == null || collection.Count == 0)
            {
                return string.Empty;
            }

            var queryString = new StringBuilder();

            foreach (var kvp in collection)
            {
                if (queryString.Length > 0)
                {
                    queryString.Append('&');
                }
                queryString.Append($"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}");
            }

            return queryString.ToString();
        }

        public static Dictionary<string, string> Clone(Dictionary<string, string>? original)
        {
            if (original == null)
            {
                return new Dictionary<string, string>();
            }

            var clone = new Dictionary<string, string>(original.Count, original.Comparer);
            foreach (var kvp in original)
            {
                clone.Add(kvp.Key, kvp.Value);
            }
            return clone;
        }
    }
}
