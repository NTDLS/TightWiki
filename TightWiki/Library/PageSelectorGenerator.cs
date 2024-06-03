using System.Text;

namespace TightWiki.Library
{
    public static class PageSelectorGenerator
    {
        public static string Generate(QueryString? queryString, int? totalPageCount)
            => Generate(string.Empty, "page", QueryStringConverter.ToDictionary(queryString), totalPageCount);

        public static string Generate(string queryToken, IQueryCollection? queryString, int? totalPageCount)
            => Generate(string.Empty, queryToken, QueryStringConverter.ToDictionary(queryString), totalPageCount);

        public static string Generate(string url, string queryToken, Dictionary<string, string>? queryString, int? totalPageCount)
        {
            var sb = new StringBuilder();
            int currentPage = 1;

            var firstPage = QueryStringConverter.Clone(queryString);
            if (firstPage.TryGetValue(queryToken, out var currentPageString))
            {
                currentPage = int.Parse(currentPageString);
            }

            firstPage.Remove(queryToken);
            firstPage.Add(queryToken, "1");

            var prevPage = QueryStringConverter.Clone(firstPage);
            prevPage.Remove(queryToken);
            prevPage.Add(queryToken, $"{currentPage - 1}");

            var nextPage = QueryStringConverter.Clone(firstPage);
            nextPage.Remove(queryToken);
            nextPage.Add(queryToken, $"{currentPage + 1}");

            var lastPage = QueryStringConverter.Clone(firstPage);
            lastPage.Remove(queryToken);
            lastPage.Add(queryToken, $"{totalPageCount}");

            if ((totalPageCount ?? 0) > 1 || currentPage > 1)
            {
                sb.Append($"<center>");
                if (currentPage > 1)
                {
                    sb.Append($"<a href=\"{url}?{QueryStringConverter.FromCollection(firstPage)}\">&lt;&lt; First</a>");
                    sb.Append("&nbsp; | &nbsp;");
                    sb.Append($"<a href=\"{url}?{QueryStringConverter.FromCollection(prevPage)}\">&lt; Previous</a>");
                }
                else
                {
                    sb.Append($"&lt;&lt; First &nbsp; | &nbsp; &lt; Previous");
                }
                sb.Append("&nbsp; | &nbsp;");

                if (currentPage < totalPageCount)
                {
                    sb.Append($"<a href=\"{url}?{QueryStringConverter.FromCollection(nextPage)}\">Next &gt;</a>");
                    sb.Append("&nbsp; | &nbsp;");
                    sb.Append($"<a href=\"{url}?{QueryStringConverter.FromCollection(lastPage)}\">Last &gt;&gt;</a>");
                }
                else
                {
                    sb.Append("Next &gt; &nbsp; | &nbsp; Last &gt;&gt;");
                }
                sb.Append($"</center>");
            }

            return sb.ToString();
        }
    }
}
