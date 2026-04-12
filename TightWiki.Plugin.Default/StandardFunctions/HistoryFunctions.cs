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
    [TwPlugin("Page History & Revisions", "Built-in standard functions.")]
    public class HistoryFunctions
    {
        [TwStandardFunctionPlugin("Revisions", "Creates a list of all revisions for a page.")]
        public async Task<TwPluginResult> Revisions(ITwEngineState state,
            TwListStyle styleName = TwListStyle.Full, int pageSize = 5, bool pageSelector = true, string? pageName = null)
        {
            if (state.Session == null)
            {
                throw new Exception($"Localization is not supported without SessionState.");
            }

            string refTag = state.GetNextTagMarker("Revisions");

            int pageNumber = int.Parse(state.QueryString[refTag].ToString().DefaultWhenNullOrEmpty("1"));

            var navigation = TwNamespaceNavigation.CleanAndValidate(pageName ?? state.Page.Navigation);
            var revisions = await state.Engine.DatabaseManager.PageRepository.GetPageRevisionsInfoByNavigationPaged(navigation, pageNumber, null, null, pageSize);
            var html = new StringBuilder();
            html.Append($"<div id=\"{refTag}\"></div>");

            if (revisions.Count > 0)
            {
                html.Append("<ul>");
                foreach (var item in revisions)
                {
                    html.Append($"<li><a href=\"{state.Engine.WikiConfiguration.BasePath}/{item.Navigation}/{item.Revision}\">{item.Revision} by {item.ModifiedByUserName} on {state.Session.LocalizeDateTime(item.ModifiedDate)}</a>");

                    if (styleName == TwListStyle.Full)
                    {
                        var thisRev = await state.Engine.DatabaseManager.PageRepository.GetPageRevisionByNavigation(state.Page.Navigation, item.Revision);
                        var prevRev = await state.Engine.DatabaseManager.PageRepository.GetPageRevisionByNavigation(state.Page.Navigation, item.Revision - 1);

                        var summaryText = TwDifferentiator.GetComparisonSummary(thisRev?.Body ?? string.Empty, prevRev?.Body ?? string.Empty);

                        if (summaryText.Length > 0)
                        {
                            html.Append(" - " + summaryText);
                        }
                    }
                    html.Append("</li>");
                }
                html.Append("</ul>");

                if (pageSelector && revisions.Count > 0 && revisions.First().PaginationPageCount > 1)
                {
                    html.Append(TwPageSelectorGenerator.Generate(state.QueryString, revisions.First().PaginationPageCount, refTag));
                }
            }

            return new TwPluginResult(html.ToString());
        }

        [TwStandardFunctionPlugin("RecentlyModified", "Creates a list of pages that have been recently modified.")]
        public async Task<TwPluginResult> RecentlyModified(ITwEngineState state,
            int top = 100, TwListStyle styleName = TwListStyle.Full, bool showNamespace = false)
        {
            if (state.Session == null)
            {
                throw new Exception($"Localization is not supported without SessionState.");
            }

            var pages = (await state.Engine.DatabaseManager.PageRepository.GetTopRecentlyModifiedPagesInfo(top))
                .OrderByDescending(o => o.ModifiedDate).ThenBy(o => o.Title).ToList();

            if (pages.Count == 0)
            {
                return new TwPluginResult(string.Empty);
            }

            var html = new StringBuilder();

            if (styleName == TwListStyle.Full)
            {
                html.Append("""<div class="tw-recent-pages">""");
                foreach (var page in pages)
                {
                    var localized = state.Session.LocalizeDateTime(state.Page.ModifiedDate);
                    html.Append($"""<div class="border-bottom">""");

                    html.Append($"""<a href="{state.Engine.WikiConfiguration.BasePath}/{page.Navigation}" class="d-block text-reset py-2">""");
                    html.Append("""<span class="fw-semibold text-truncate">""");
                    html.Append(showNamespace ? page.Name : page.Title);
                    html.Append("""</span>""");
                    html.Append("""<span class="small text-body-secondary text-nowrap ms-2">""");
                    html.Append($"({localized.ToShortDateString()} {localized.ToShortTimeString()})");
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

                html.Append("""</div>""");
            }
            else if (styleName == TwListStyle.List)
            {
                html.Append("<ul>");
                foreach (var page in pages)
                {
                    if (showNamespace)
                    {
                        html.Append($"<li><a href=\"{state.Engine.WikiConfiguration.BasePath}/{page.Navigation}\">{page.Name}</a>");
                    }
                    else
                    {
                        html.Append($"<li><a href=\"{state.Engine.WikiConfiguration.BasePath}/{page.Navigation}\">{page.Title}</a>");
                    }

                    if (page?.Description?.Length > 0)
                    {
                        html.Append(" - " + page.Description);
                    }
                    html.Append("</li>");
                }
                html.Append("</ul>");
            }

            return new TwPluginResult(html.ToString())
            {
                Instructions = [TwResultInstruction.DisallowNestedProcessing]
            };
        }

        [TwStandardFunctionPlugin("RecentlyCreated", "Creates a list of pages that have been recently created.")]
        public async Task<TwPluginResult> RecentlyCreated(ITwEngineState state,
            int top = 100, TwListStyle styleName = TwListStyle.Full, bool showNamespace = false)
        {
            if (state.Session == null)
            {
                throw new Exception($"Localization is not supported without SessionState.");
            }

            var pages = (await state.Engine.DatabaseManager.PageRepository.GetTopRecentlyModifiedPagesInfo(top))
                .OrderByDescending(o => o.ModifiedDate).ThenBy(o => o.Title).ToList();

            if (pages.Count == 0)
            {
                return new TwPluginResult(string.Empty);
            }

            var html = new StringBuilder();

            if (styleName == TwListStyle.Full)
            {
                html.Append("""<div class="tw-recent-pages">""");
                foreach (var page in pages)
                {
                    var localized = state.Session.LocalizeDateTime(state.Page.CreatedDate);
                    html.Append($"""<div class="border-bottom">""");

                    html.Append($"""<a href="{state.Engine.WikiConfiguration.BasePath}/{page.Navigation}" class="d-block text-reset py-2">""");
                    html.Append("""<span class="fw-semibold text-truncate">""");
                    html.Append(showNamespace ? page.Name : page.Title);
                    html.Append("""</span>""");
                    html.Append("""<span class="small text-body-secondary text-nowrap ms-2">""");
                    html.Append($"({localized.ToShortDateString()} {localized.ToShortTimeString()})");
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

                html.Append("""</div>""");
            }
            else if (styleName == TwListStyle.List)
            {
                html.Append("<ul>");
                foreach (var page in pages)
                {
                    if (showNamespace)
                    {
                        html.Append($"<li><a href=\"{state.Engine.WikiConfiguration.BasePath}/{page.Navigation}\">{page.Name}</a>");
                    }
                    else
                    {
                        html.Append($"<li><a href=\"{state.Engine.WikiConfiguration.BasePath}/{page.Navigation}\">{page.Title}</a>");
                    }

                    if (page?.Description?.Length > 0)
                    {
                        html.Append(" - " + page.Description);
                    }
                    html.Append("</li>");
                }
                html.Append("</ul>");
            }

            return new TwPluginResult(html.ToString())
            {
                Instructions = [TwResultInstruction.DisallowNestedProcessing]
            };
        }

        [TwStandardFunctionPlugin("MostEdited", "Creates a list of pages that have been most edited.")]
        public async Task<TwPluginResult> MostEdited(ITwEngineState state,
            int top = 100, TwListStyle styleName = TwListStyle.Full, bool showNamespace = false)
        {
            var pages = (await state.Engine.DatabaseManager.PageRepository.GetTopEditedPagesInfo(top))
                .OrderByDescending(o => o.Revision).ThenBy(o => o.Title).ToList();

            if (pages.Count == 0)
            {
                return new TwPluginResult(string.Empty);
            }

            var html = new StringBuilder();

            if (styleName == TwListStyle.Full)
            {
                html.Append("""<div class="tw-recent-pages">""");
                foreach (var page in pages)
                {
                    html.Append($"""<div class="border-bottom">""");

                    html.Append($"""<a href="{state.Engine.WikiConfiguration.BasePath}/{page.Navigation}" class="d-block text-reset py-2">""");
                    html.Append("""<span class="fw-semibold text-truncate">""");
                    html.Append(showNamespace ? page.Name : page.Title);
                    html.Append("""</span>""");
                    html.Append("""<span class="small text-body-secondary text-nowrap ms-2">""");
                    html.Append($"({page.Revision:n0})");
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

                html.Append("""</div>""");
            }
            else if (styleName == TwListStyle.List)
            {
                html.Append("<ul>");
                foreach (var page in pages)
                {
                    if (showNamespace)
                    {
                        html.Append($"<li><a href=\"{state.Engine.WikiConfiguration.BasePath}/{page.Navigation}\">{page.Name} ({page.TotalViewCount:n0})</a>");
                    }
                    else
                    {
                        html.Append($"<li><a href=\"{state.Engine.WikiConfiguration.BasePath}/{page.Navigation}\">{page.Title} ({page.TotalViewCount:n0})</a>");
                    }
                    html.Append("</li>");
                }
                html.Append("</ul>");
            }

            return new TwPluginResult(html.ToString())
            {
                Instructions = [TwResultInstruction.DisallowNestedProcessing]
            };
        }

        [TwStandardFunctionPlugin("MostViewed", "Creates a list of pages that have been most viewed.")]
        public async Task<TwPluginResult> MostViewed(ITwEngineState state,
            int top = 100, TwListStyle styleName = TwListStyle.Full, bool showNamespace = false)
        {
            var pages = (await state.Engine.DatabaseManager.PageRepository.GetTopViewedPagesInfo(top))
                .OrderByDescending(o => o.TotalViewCount).ThenBy(o => o.Title).ToList();

            if (pages.Count == 0)
            {
                return new TwPluginResult(string.Empty);
            }

            var html = new StringBuilder();

            if (styleName == TwListStyle.Full)
            {
                html.Append("""<div class="tw-recent-pages">""");
                foreach (var page in pages)
                {
                    html.Append($"""<div class="border-bottom">""");

                    html.Append($"""<a href="{state.Engine.WikiConfiguration.BasePath}/{page.Navigation}" class="d-block text-reset py-2">""");
                    html.Append("""<span class="fw-semibold text-truncate">""");
                    html.Append(showNamespace ? page.Name : page.Title);
                    html.Append("""</span>""");
                    html.Append("""<span class="small text-body-secondary text-nowrap ms-2">""");
                    html.Append($"({page.TotalViewCount:n0})");
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

                html.Append("""</div>""");
            }
            else if (styleName == TwListStyle.List)
            {
                html.Append("<ul>");
                foreach (var page in pages)
                {
                    if (showNamespace)
                    {
                        html.Append($"<li><a href=\"{state.Engine.WikiConfiguration.BasePath}/{page.Navigation}\">{page.Name} ({page.TotalViewCount:n0})</a>");
                    }
                    else
                    {
                        html.Append($"<li><a href=\"{state.Engine.WikiConfiguration.BasePath}/{page.Navigation}\">{page.Title} ({page.TotalViewCount:n0})</a>");
                    }
                    html.Append("</li>");
                }
                html.Append("</ul>");
            }

            return new TwPluginResult(html.ToString())
            {
                Instructions = [TwResultInstruction.DisallowNestedProcessing]
            };
        }
    }
}
