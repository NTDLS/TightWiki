using Microsoft.AspNetCore.Html;
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
                return new HtmlString($@"
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
                    </div>");
            }

            return new HtmlString(string.Empty);
        }
    }
}
