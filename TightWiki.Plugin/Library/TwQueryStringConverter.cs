using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Web;
using TightWiki.Plugin.Interfaces;

namespace TightWiki.Plugin.Library
{
    /// <summary>
    /// Provides utility methods for manipulating and converting query strings, including upserting key-value pairs,
    /// handling order-by parameters, and converting between different query string representations.
    /// </summary>
    /// <remarks>This static class is intended for use in web applications where query string manipulation is
    /// required, such as sorting, filtering, or updating URL parameters. All methods are thread-safe and do not modify
    /// input collections in place. The class supports conversion between various query string formats, including
    /// string, dictionary, and IQueryCollection representations.</remarks>
    public static class TwQueryStringConverter
    {
        /// <summary>
        /// Takes the current page query string and "upserts" the given order-by field,
        /// if the string already sorts on the given field then the order is inverted (asc/desc).
        /// </summary>
        public static IHtmlContent OrderHelper(ITwSessionState context, string value, string keySuffix = "")
        {
            string orderByKey = (string.IsNullOrEmpty(keySuffix) ? "OrderBy" : $"OrderBy_{keySuffix}");
            string orderByDirectionKey = (string.IsNullOrEmpty(keySuffix) ? "OrderByDirection" : $"OrderByDirection_{keySuffix}");
            string? currentDirection = "asc";
            var collection = ToDictionary(context.QueryString);

            //Check to see if we are sorting on the value that we are already sorted on, this would mean we need to invert the sort.
            if (collection.TryGetValue(orderByKey, out var currentValue))
            {
                bool invertDirection = string.Equals(currentValue, value, StringComparison.InvariantCultureIgnoreCase);

                if (invertDirection)
                {
                    if (collection.TryGetValue(orderByDirectionKey, out currentDirection))
                    {
                        if (currentDirection == "asc")
                        {
                            currentDirection = "desc";
                        }
                        else
                        {
                            currentDirection = "asc";
                        }
                    }
                }
                else
                {
                    currentDirection = "asc";
                }
            }

            collection.Remove(orderByKey);
            collection.Add(orderByKey, value);

            collection.Remove(orderByDirectionKey);
            collection.Add(orderByDirectionKey, currentDirection ?? "asc");

            return new HtmlString(FromCollection(collection));
        }

        /// <summary>
        /// Takes the current page query string and "upserts" a query key/value, replacing any conflicting query string entry.
        /// </summary>
        public static string Upsert(IQueryCollection? queryString, string name, string value)
        {
            var collection = ToDictionary(queryString);
            collection.Remove(name);
            collection.Add(name, value);
            return FromCollection(collection);
        }

        /// <summary>
        /// Converts the specified query string collection to a dictionary of key-value pairs.
        /// </summary>
        /// <remarks>If a key in the query string collection has multiple values, an exception is thrown.
        /// Keys are compared using a case-insensitive comparer based on the invariant culture.</remarks>
        /// <param name="queryString">The query string collection to convert. If null, an empty dictionary is returned.</param>
        /// <returns>A dictionary containing the keys and single values from the query string collection. If the collection is
        /// null, returns an empty dictionary.</returns>
        public static Dictionary<string, string> ToDictionary(IQueryCollection? queryString)
        {
            if (queryString == null)
            {
                return new Dictionary<string, string>();
            }

            var dictionary = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var item in queryString)
            {
                //Technically, keys can be duplicated in a IQueryCollection but we do not
                //support this. Use .Single() to throw exception if duplicates are found.
                dictionary.Add(item.Key, item.Value.Single() ?? string.Empty);
            }

            return dictionary;
        }

        /// <summary>
        /// Converts the specified query string into a dictionary of key-value pairs.
        /// </summary>
        /// <param name="queryString">The query string to convert. Can be null or empty.</param>
        /// <returns>A dictionary containing the key-value pairs parsed from the query string. Returns an empty dictionary if the
        /// query string is null or empty.</returns>
        public static Dictionary<string, string> ToDictionary(QueryString? queryString)
            => ToDictionary(queryString?.ToString());

        /// <summary>
        /// Converts a URL query string into a dictionary of key-value pairs.
        /// </summary>
        /// <remarks>If the query string contains duplicate keys, the last occurrence is used. Keys and
        /// values are URL-decoded. Only pairs with both a key and a value are included in the result.</remarks>
        /// <param name="queryString">The query string to parse. May include a leading question mark ('?'). If null or empty, an empty dictionary
        /// is returned.</param>
        /// <returns>A dictionary containing the decoded key-value pairs from the query string. The dictionary is
        /// case-insensitive and will be empty if the input is null or empty.</returns>
        public static Dictionary<string, string> ToDictionary(string? queryString)
        {
            var dictionary = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
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

        /// <summary>
        /// Creates a URL-encoded query string from the specified collection of query parameters.
        /// </summary>
        /// <remarks>Each key and value in the collection is URL-encoded. Parameters are concatenated
        /// using the ampersand character. If the collection contains multiple values for a key, only the first value is
        /// included.</remarks>
        /// <param name="collection">The collection of query parameters to include in the query string. Can be null or empty.</param>
        /// <returns>A URL-encoded query string representing the parameters in the collection, or an empty string if the
        /// collection is null or contains no elements.</returns>
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

        /// <summary>
        /// Creates a URL-encoded query string from the specified collection of key-value pairs.
        /// </summary>
        /// <remarks>Each key-value pair is formatted as 'key=value' and pairs are separated by an
        /// ampersand. Both keys and values are URL-encoded using UTF-8 encoding.</remarks>
        /// <param name="collection">A dictionary containing the keys and values to include in the query string. Keys and values are URL-encoded.
        /// Can be null.</param>
        /// <returns>A URL-encoded query string representing the contents of the collection, or an empty string if the collection
        /// is null or empty.</returns>
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

        /// <summary>
        /// Creates a shallow copy of the specified dictionary of string keys and values.
        /// </summary>
        /// <remarks>The returned dictionary uses the same key comparer as the original dictionary. The
        /// copy is shallow; string values are not cloned.</remarks>
        /// <param name="original">The dictionary to clone. Can be null.</param>
        /// <returns>A new dictionary containing the same key-value pairs as the original. If the original is null, returns an
        /// empty dictionary.</returns>
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
