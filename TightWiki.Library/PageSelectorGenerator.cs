using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;

namespace TightWiki.Library
{
    public static class PageSelectorGenerator
    {
        public static IHtmlContent Generate(QueryString? queryString, int? totalPageCount)
            => Generate(string.Empty, QueryStringConverter.ToDictionary(queryString), totalPageCount, "page");

        public static IHtmlContent Generate(QueryString? queryString, int? totalPageCount, string queryToken)
            => Generate(string.Empty, QueryStringConverter.ToDictionary(queryString), totalPageCount, queryToken);

        public static IHtmlContent Generate(IQueryCollection? queryString, int? totalPageCount, string queryToken)
            => Generate(string.Empty, QueryStringConverter.ToDictionary(queryString), totalPageCount, queryToken);

        public static IHtmlContent Generate(string url, Dictionary<string, string>? queryString, int? totalPageCount, string queryToken)
        {
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
                var html = $@"
                    <div class='d-flex justify-content-center'>
                        <div class='btn-group' role='group'>
                            <a class='btn btn-outline-secondary {(currentPage > 1 ? "" : "disabled")}' href='{url}?{QueryStringConverter.FromCollection(firstPage)}'>
                                <i class='bi bi-chevron-double-left'></i>
                            </a>
                            <a class='btn btn-outline-secondary {(currentPage > 1 ? "" : "disabled")}' href='{url}?{QueryStringConverter.FromCollection(prevPage)}'>
                                <i class='bi bi-chevron-left'></i>
                            </a>
                            <a class='btn btn-outline-secondary {(currentPage < totalPageCount ? "" : "disabled")}' href='{url}?{QueryStringConverter.FromCollection(nextPage)}'>
                                <i class='bi bi-chevron-right'></i>
                            </a>
                            <a class='btn btn-outline-secondary {(currentPage < totalPageCount ? "" : "disabled")}' href='{url}?{QueryStringConverter.FromCollection(lastPage)}'>
                                <i class='bi bi-chevron-double-right'></i>
                            </a>
                        </div>
                    </div>";

                //When used in a wiki page. the result of the pager will be subject of the wikifier and the new-lines will be processed as <br />.
                //To avoid this, we remove all new-lines from the generated HTML.
                return new HtmlString(html.Trim().Replace("\n", "").Replace("\r", ""));
            }

            return new HtmlString(string.Empty);
        }
    }
}
