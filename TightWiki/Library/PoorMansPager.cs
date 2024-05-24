using System.Text;

namespace TightWiki.Library
{
    public static class PoorMansPager
    {
        public static string Generate(string url, int? totalPageCount, int? currentPage)
            => Generate(url, string.Empty, totalPageCount, currentPage);

        public static string Generate(string url, QueryString? queryString, int? totalPageCount, int? currentPage)
            => Generate(url, queryString.ToString(), totalPageCount, currentPage);

        public static string Generate(string url, string? queryString, int? totalPageCount, int? currentPage)
        {
            var sb = new StringBuilder();

            currentPage ??= 1;

            queryString = string.IsNullOrWhiteSpace(queryString) ? string.Empty : $"?{queryString.Trim([' ', '?'])}";

            if ((totalPageCount ?? 0) > 1 || (currentPage ?? 0) > 1)
            {
                sb.AppendLine($"<center>");
                if (currentPage > 1)
                {
                    sb.AppendLine($"<a href=\"{url}/1{queryString}\">&lt;&lt; First</a>");
                    sb.AppendLine("&nbsp; | &nbsp;");
                    sb.AppendLine($"<a href=\"{url}/{currentPage - 1}{queryString}\">&lt; Previous</a>");
                }
                else
                {
                    sb.AppendLine($"&lt;&lt; First &nbsp; | &nbsp; &lt; Previous");
                }
                sb.AppendLine("&nbsp; | &nbsp;");

                if (currentPage < totalPageCount)
                {
                    sb.AppendLine($"<a href=\"{url}/{currentPage + 1}{queryString}\">Next &gt;</a>");
                    sb.AppendLine("&nbsp; | &nbsp;");
                    sb.AppendLine($"<a href=\"{url}/{totalPageCount}{queryString}\">Last &gt;&gt;</a>");
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
