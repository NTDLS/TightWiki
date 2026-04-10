using NTDLS.Helpers;
using System.Text;
using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Attributes.Functions;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Library;
using TightWiki.Plugin.Models;

namespace TightWiki.Plugin.Default.StandardFunctions
{
    [TwPlugin("Relationships & Navigation", "Built-in standard functions.")]
    public class NavigationFunctions
    {
        [TwStandardFunctionPlugin("Similar", "Displays a list of other related pages based on tags.")]
        public async Task<TwPluginResult> Similar(ITwEngineState state,
            int similarity = 80, TwTabularStyle styleName = TwTabularStyle.Full, int pageSize = 10, bool pageSelector = true)
        {
            string refTag = state.GetNextTagMarker("Similar");

            int pageNumber = int.Parse(state.QueryString[refTag].ToString().DefaultWhenNullOrEmpty("1"));
            var html = new StringBuilder();

            var pages = await state.Engine.DatabaseManager.PageRepository.GetSimilarPagesPaged(state.Page.Id, similarity, pageNumber, pageSize);

            switch (styleName)
            {
                case TwTabularStyle.List:
                    html.Append("<ul>");
                    foreach (var page in pages)
                    {
                        html.Append($"<li><a href=\"{state.Engine.WikiConfiguration.BasePath}/{page.Navigation}\">{page.Title}</a>");
                    }
                    html.Append("</ul>");
                    break;
                case TwTabularStyle.Flat:
                    foreach (var page in pages)
                    {
                        if (html.Length > 0) html.Append(" | ");
                        html.Append($"<a href=\"{state.Engine.WikiConfiguration.BasePath}/{page.Navigation}\">{page.Title}</a>");
                    }
                    break;
                case TwTabularStyle.Full:
                    foreach (var page in pages)
                    {
                        html.Append($"""<div class="border-bottom">""");

                        html.Append($"""<a href="{state.Engine.WikiConfiguration.BasePath}/{page.Navigation}" class="d-block text-reset py-2">""");
                        html.Append("""<span class="fw-semibold text-truncate">""");
                        html.Append(page.Name);
                        html.Append("""</span>""");
                        html.Append("""</a>""");

                        if (!string.IsNullOrWhiteSpace(page.Description))
                        {
                            html.Append("""<div class="small text-body-secondary text-truncate">""");
                            html.Append($"""{page.Description}""");
                            html.Append("""</div>""");
                        }
                        html.Append("""</div>""");
                    }
                    break;
            }

            if (pageSelector && pages.Count > 0 && pages.First().PaginationPageCount > 1)
            {
                html.Append(TwPageSelectorGenerator.Generate(state.QueryString, pages.First().PaginationPageCount, refTag));
            }

            return new TwPluginResult(html.ToString());
        }

        [TwStandardFunctionPlugin("Related", "Displays a list of other related pages based incoming links.")]
        public async Task<TwPluginResult> Related(ITwEngineState state,
            TwTabularStyle styleName = TwTabularStyle.Full, int pageSize = 10, bool pageSelector = true)
        {
            string refTag = state.GetNextTagMarker("Related");

            int pageNumber = int.Parse(state.QueryString[refTag].ToString().DefaultWhenNullOrEmpty("1"));
            var html = new StringBuilder();

            var pages = await state.Engine.DatabaseManager.PageRepository.GetRelatedPagesPaged(state.Page.Id, pageNumber, pageSize);

            switch (styleName)
            {
                case TwTabularStyle.List:
                    html.Append("<ul>");
                    foreach (var page in pages)
                    {
                        html.Append($"<li><a href=\"{state.Engine.WikiConfiguration.BasePath}/{page.Navigation}\">{page.Title}</a>");
                    }
                    html.Append("</ul>");
                    break;
                case TwTabularStyle.Flat:
                    foreach (var page in pages)
                    {
                        if (html.Length > 0) html.Append(" | ");
                        html.Append($"<a href=\"{state.Engine.WikiConfiguration.BasePath}/{page.Navigation}\">{page.Title}</a>");
                    }
                    break;
                case TwTabularStyle.Full:
                    foreach (var page in pages)
                    {
                        html.Append($"""<div class="border-bottom">""");

                        html.Append($"""<a href="{state.Engine.WikiConfiguration.BasePath}/{page.Navigation}" class="d-block text-reset py-2">""");
                        html.Append("""<span class="fw-semibold text-truncate">""");
                        html.Append(page.Name);
                        html.Append("""</span>""");
                        html.Append("""</a>""");

                        if (!string.IsNullOrWhiteSpace(page.Description))
                        {
                            html.Append("""<div class="small text-body-secondary text-truncate">""");
                            html.Append($"""{page.Description}""");
                            html.Append("""</div>""");
                        }
                        html.Append("""</div>""");
                    }
                    break;
            }

            if (pageSelector && pages.Count > 0 && pages.First().PaginationPageCount > 1)
            {
                html.Append(TwPageSelectorGenerator.Generate(state.QueryString, pages.First().PaginationPageCount, refTag));
            }

            return new TwPluginResult(html.ToString());
        }

        [TwStandardFunctionPlugin("EditLink", "Creates an edit link for the current page.")]
        public async Task<TwPluginResult> EditLink(ITwEngineState state, string linkText = "edit")
        {
            return new TwPluginResult($"<a href=\"{state.Engine.WikiConfiguration.BasePath}"
                + TwNamespaceNavigation.CleanAndValidate($"/{state.Page.Navigation}/Edit") + $"\">{linkText}</a>");
        }

        [TwStandardFunctionPlugin("Navigation", "Displays the navigation text for the current page.")]
        public async Task<TwPluginResult> Navigation(ITwEngineState state)
        {
            return new TwPluginResult(state.Page.Navigation);
        }
    }
}
