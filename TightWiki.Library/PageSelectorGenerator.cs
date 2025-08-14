using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using System.Text;

namespace TightWiki.Library
{
    public static class PageSelectorGenerator
    {
        public static IStringLocalizer Localizer
        {
            get
            {
                if (_localizer == null)
                {
                    throw new InvalidOperationException("StaticHelper has not been initialized. Call StaticHelper.Initializer() with a valid IStringLocalizer instance.");
                }
                return _localizer;
            }
        }
        private static IStringLocalizer? _localizer;

        public static void Initialize(IStringLocalizer localizer)
        {
            _localizer = localizer;
        }

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
                    sb.Append($"<a href=\"{url}?{QueryStringConverter.FromCollection(firstPage)}\">&lt;&lt; {Localizer["First"]}</a>");
                    sb.Append("&nbsp; | &nbsp;");
                    sb.Append($"<a href=\"{url}?{QueryStringConverter.FromCollection(prevPage)}\">&lt; {Localizer["Previous"]}</a>");
                }
                else
                {
                    sb.Append($"&lt;&lt; {Localizer["First"]} &nbsp; | &nbsp; &lt; {Localizer["Previous"]}");
                }
                sb.Append("&nbsp; | &nbsp;");

                if (currentPage < totalPageCount)
                {
                    sb.Append($"<a href=\"{url}?{QueryStringConverter.FromCollection(nextPage)}\">{Localizer["Next"]} &gt;</a>");
                    sb.Append("&nbsp; | &nbsp;");
                    sb.Append($"<a href=\"{url}?{QueryStringConverter.FromCollection(lastPage)}\">{Localizer["Last"]} &gt;&gt;</a>");
                }
                else
                {
                    sb.Append($"{Localizer["Next"]} &gt; &nbsp; | &nbsp; {Localizer["Last"]} &gt;&gt;");
                }
                sb.Append($"</center>");
            }

            return sb.ToString();
        }
    }
}
