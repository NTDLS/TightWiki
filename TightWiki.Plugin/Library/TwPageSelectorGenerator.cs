using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using System.Net;
using TightWiki.Plugin.Interfaces;

namespace TightWiki.Plugin.Library
{
    /// <summary>
    /// Provides static methods for generating HTML pagination controls using Tailwind CSS-compatible markup for use in
    /// ASP.NET Core applications.
    /// </summary>
    /// <remarks>This class is intended to simplify the creation of page navigation elements that support
    /// query string manipulation for paging scenarios. The generated HTML is suitable for integration with Bootstrap
    /// icon classes and can be customized with additional CSS classes. All methods return an implementation of
    /// IHtmlContent, making them suitable for direct use in Razor views. The class is thread-safe as it contains only
    /// stateless static methods.</remarks>
    public static class TwPageSelectorGenerator
    {
        /// <summary>
        /// Generates an HTML pagination control based on the specified query string and total page count.
        /// </summary>
        /// <param name="queryString">The query string containing the current request parameters. Can be null to indicate no query parameters.</param>
        /// <param name="totalPageCount">The total number of pages to display in the pagination control. Can be null if the total page count is
        /// unknown.</param>
        /// <param name="class">An optional CSS class to apply to the pagination control. Can be null to omit the class attribute.</param>
        /// <returns>An object that represents the generated HTML content for the pagination control.</returns>
        public static IHtmlContent Generate(QueryString? queryString, int? totalPageCount, string? @class = null, ITwSharedLocalizationText? localizer = null)
            => Generate(string.Empty, TwQueryStringConverter.ToDictionary(queryString), totalPageCount, "page", @class, null, localizer);

        /// <summary>
        /// Generates an HTML pagination control based on the specified query string, total page count, and query token.
        /// </summary>
        /// <param name="queryString">The query string containing the current request parameters. Can be null to indicate no query parameters.</param>
        /// <param name="totalPageCount">The total number of pages to display in the pagination control. Can be null if the page count is unknown.</param>
        /// <param name="queryToken">The name of the query parameter used to represent the page number in generated links. Cannot be null.</param>
        /// <param name="class">An optional CSS class to apply to the pagination control. Can be null to omit the class attribute.</param>
        /// <returns>An <see cref="IHtmlContent"/> instance representing the rendered pagination control.</returns>
        public static IHtmlContent Generate(QueryString? queryString, int? totalPageCount, string queryToken, string? @class = null, ITwSharedLocalizationText? localizer = null)
            => Generate(string.Empty, TwQueryStringConverter.ToDictionary(queryString), totalPageCount, queryToken, @class, null, localizer);

        /// <summary>
        /// Generates an HTML pagination control based on the specified query string, total page count, and query token.
        /// </summary>
        /// <param name="queryString">The query string parameters from the current HTTP request. Can be null if no query parameters are present.</param>
        /// <param name="totalPageCount">The total number of pages to display in the pagination control. If null, the pagination control will not be
        /// rendered.</param>
        /// <param name="queryToken">A token used to identify the page number parameter in the generated query string.</param>
        /// <param name="class">An optional CSS class to apply to the pagination control. If null, no additional class is added.</param>
        /// <param name="anchor">Anchor to scroll to when the pager is clicked.</param>
        /// <returns>An <see cref="IHtmlContent"/> instance representing the rendered pagination control. Returns an empty
        /// content if <paramref name="totalPageCount"/> is null.</returns>
        public static IHtmlContent Generate(IQueryCollection? queryString, int? totalPageCount, string queryToken, string? @class = null, string? anchor = null, ITwSharedLocalizationText? localizer = null)
            => Generate(string.Empty, TwQueryStringConverter.ToDictionary(queryString), totalPageCount, queryToken, @class, anchor, localizer);

        /// <summary>
        /// Generates an HTML pagination control for navigating between pages in a web application.
        /// </summary>
        /// <remarks>The generated HTML uses Bootstrap classes for styling and includes navigation buttons
        /// for first, previous, next, and last pages. Disabled states are applied when navigation is not possible. All
        /// newlines are removed from the output to prevent unwanted formatting when used in contexts such as wiki
        /// pages.</remarks>
        /// <param name="url">The base URL to which pagination query parameters will be appended.</param>
        /// <param name="queryString">A dictionary containing the current query string parameters. Can be null if no parameters are present.</param>
        /// <param name="totalPageCount">The total number of pages available. If null, pagination controls for next and last pages may be disabled.</param>
        /// <param name="queryToken">The name of the query string parameter that represents the current page number.</param>
        /// <param name="class">An optional CSS class to apply to the pagination container. Can be null.</param>
        /// <param name="anchor">Anchor to scroll to when the pager is clicked.</param>
        /// <returns>An <see cref="IHtmlContent"/> instance containing the rendered HTML for the pagination control. Returns an
        /// empty HTML string if there is only one page and the current page is the first.</returns>
        private static IHtmlContent Generate(string url, Dictionary<string, string>? queryString,
            int? totalPageCount, string queryToken, string? @class = null, string? anchor = null, ITwSharedLocalizationText? localizer = null)
        {
            string Text(string key, params object[] args)
                => WebUtility.HtmlEncode(localizer != null ? localizer.Format(key, args) : string.Format(key, args));

            int currentPage = 1;

            var firstPage = TwQueryStringConverter.Clone(queryString);
            if (firstPage.TryGetValue(queryToken, out var currentPageString))
            {
                currentPage = int.Parse(currentPageString);
            }

            firstPage.Remove(queryToken);
            firstPage.Add(queryToken, "1");

            var prevPage = TwQueryStringConverter.Clone(firstPage);
            prevPage.Remove(queryToken);
            prevPage.Add(queryToken, $"{currentPage - 1}");

            var nextPage = TwQueryStringConverter.Clone(firstPage);
            nextPage.Remove(queryToken);
            nextPage.Add(queryToken, $"{currentPage + 1}");

            var lastPage = TwQueryStringConverter.Clone(firstPage);
            lastPage.Remove(queryToken);
            lastPage.Add(queryToken, $"{totalPageCount}");

            var fragment = anchor != null ? $"#{anchor}" : $"#{queryToken}";

            if ((totalPageCount ?? 0) > 1 || currentPage > 1)
            {
                bool hasPrevious = currentPage > 1;
                bool hasNext = totalPageCount != null && currentPage < totalPageCount;

                //Disabled buttons are also taken out of the keyboard order and announced as disabled.
                string Button(bool enabled, Dictionary<string, string> target, string icon, string label)
                    => $"<a class='btn btn-outline-secondary{(enabled ? "" : " disabled")}' href='{url}?{TwQueryStringConverter.FromCollection(target)}{fragment}'"
                     + $" aria-label='{label}' title='{label}'{(enabled ? "" : " aria-disabled='true' tabindex='-1'")}><i class='bi {icon}' aria-hidden='true'></i></a>";

                string position = totalPageCount != null
                    ? Text("Page {0} of {1}", currentPage, totalPageCount)
                    : Text("Page {0}", currentPage);

                var html = $@"
                <nav class='d-flex justify-content-center {@class ?? string.Empty}' aria-label='{Text("Pagination")}'>
                    <div class='btn-group' role='group'>
                        {Button(hasPrevious, firstPage, "bi-chevron-double-left", Text("First page"))}
                        {Button(hasPrevious, prevPage, "bi-chevron-left", Text("Previous page"))}
                        <span class='btn btn-outline-secondary disabled' aria-current='page'>{position}</span>
                        {Button(hasNext, nextPage, "bi-chevron-right", Text("Next page"))}
                        {Button(hasNext, lastPage, "bi-chevron-double-right", Text("Last page"))}
                    </div>
                </nav>";

                return new HtmlString(html.Trim().Replace("\n", "").Replace("\r", ""));
            }

            return new HtmlString(string.Empty);
        }
    }
}
