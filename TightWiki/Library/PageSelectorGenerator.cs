using System.Text;

namespace TightWiki.Library
{
    public static class PageSelectorGenerator
    {
        public static string Generate(string url, QueryString? queryString, int? totalPageCount)
            => Generate(url, QueryStringConverter.ToDictionary(queryString), totalPageCount);

        public static string Generate(string url, IQueryCollection? queryString, int? totalPageCount)
            => Generate(url, QueryStringConverter.ToDictionary(queryString), totalPageCount);

        public static string Generate(string url, Dictionary<string, string>? queryString, int? totalPageCount)
        {
            var sb = new StringBuilder();
            int currentPage = 1;

            var firstPage = QueryStringConverter.Clone(queryString);
            if (firstPage.TryGetValue("page", out var currentPageString))
            {
                currentPage = int.Parse(currentPageString);
            }

            firstPage.Remove("page");
            firstPage.Add("page", "1");

            var prevPage = QueryStringConverter.Clone(firstPage);
            prevPage.Remove("page");
            prevPage.Add("page", $"{currentPage - 1}");

            var nextPage = QueryStringConverter.Clone(firstPage);
            nextPage.Remove("page");
            nextPage.Add("page", $"{currentPage + 1}");

            var lastPage = QueryStringConverter.Clone(firstPage);
            lastPage.Remove("page");
            lastPage.Add("page", $"{totalPageCount}");

            if ((totalPageCount ?? 0) > 1 || currentPage > 1)
            {
                sb.AppendLine($"<center>");
                if (currentPage > 1)
                {
                    sb.AppendLine($"<a href=\"{url}?{QueryStringConverter.FromCollection(firstPage)}\">&lt;&lt; First</a>");
                    sb.AppendLine("&nbsp; | &nbsp;");
                    sb.AppendLine($"<a href=\"{url}?{QueryStringConverter.FromCollection(prevPage)}\">&lt; Previous</a>");
                }
                else
                {
                    sb.AppendLine($"&lt;&lt; First &nbsp; | &nbsp; &lt; Previous");
                }
                sb.AppendLine("&nbsp; | &nbsp;");

                if (currentPage < totalPageCount)
                {
                    sb.AppendLine($"<a href=\"{url}?{QueryStringConverter.FromCollection(nextPage)}\">Next &gt;</a>");
                    sb.AppendLine("&nbsp; | &nbsp;");
                    sb.AppendLine($"<a href=\"{url}?{QueryStringConverter.FromCollection(lastPage)}\">Last &gt;&gt;</a>");
                }
                else
                {
                    sb.AppendLine("Next &gt; &nbsp; | &nbsp; Last &gt;&gt;");
                }
                sb.AppendLine($"</center>");
            }

            return sb.ToString();
        }
    }
}
