using NTDLS.Helpers;
using System.Text;
using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Attributes.Functions;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Library;
using TightWiki.Plugin.Models;

namespace TightWiki.Plugin.Default.StandardFunctions
{
    [TwPlugin("Content Discovery & Lists", "Built-in standard functions.")]
    public class ContentFunctions
    {
        #region Helpers.

        private async Task<TwPage?> GetPageFromNavigation(ITwPageRepository pageRepository, string routeData)
        {
            routeData = TwNamespaceNavigation.CleanAndValidate(routeData);
            var page = await pageRepository.GetPageRevisionByNavigation(routeData);
            return page;
        }

        private static void MergeUserVariables(ref ITwEngineState state, Dictionary<string, string> items)
        {
            foreach (var item in items)
            {
                state.Variables[item.Key] = item.Value;
            }
        }

        private static void MergeSnippets(ref ITwEngineState state, Dictionary<string, string> items)
        {
            foreach (var item in items)
            {
                state.Snippets[item.Key] = item.Value;
            }
        }

        #endregion

        [TwStandardFunctionPlugin("Tags", "Displays list of tag links for the tags that are included on the current page.")]
        public async Task<TwPluginResult> Tags(ITwEngineState state,
            TwTabularStyle styleName)
        {
            var html = new StringBuilder();

            switch (styleName)
            {
                case TwTabularStyle.List:
                case TwTabularStyle.Full:
                    html.Append("<ul>");
                    foreach (var tag in state.Tags)
                    {
                        html.Append($"<li><a href=\"{state.Engine.WikiConfiguration.BasePath}/Tags/Browse/{tag}\">{tag}</a>");
                    }
                    html.Append("</ul>");
                    break;
                case TwTabularStyle.Flat:
                    foreach (var tag in state.Tags)
                    {
                        if (html.Length > 0) html.Append(" | ");
                        html.Append($"<a href=\"{state.Engine.WikiConfiguration.BasePath}/Tags/Browse/{tag}\">{tag}</a>");
                    }
                    break;
            }

            return new TwPluginResult(html.ToString());
        }

        [TwStandardFunctionPlugin("TagCloud", "Displays a tag cloud for the specified page tag.")]
        public async Task<TwPluginResult> TagCloud(ITwEngineState state,
            string pageTag, int top = 1000)
        {
            string html = await TwTagCloudBuilder.Build(state.Engine.DatabaseManager.PageRepository, state.Engine.WikiConfiguration.BasePath, pageTag, top);
            return new TwPluginResult(html);
        }

        [TwStandardFunctionPlugin("SearchCloud", "Displays a search cloud for the specified search phrase.")]
        public async Task<TwPluginResult> SearchCloud(ITwEngineState state,
            string searchPhrase, int top = 1000)
        {
            var tokens = searchPhrase.Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList();

            string html = await TwSearchCloudBuilder.Build(state.Engine.DatabaseManager.PageRepository, state.Engine.WikiConfiguration.BasePath, tokens, top);
            return new TwPluginResult(html);
        }

        [TwStandardFunctionPlugin("TagList", "Creates a list of pages by searching the page tags.")]
        public async Task<TwPluginResult> TagList(ITwEngineState state,
            string[] pageTags, int top = 1000, TwListStyle styleName = TwListStyle.Full, bool showNamespace = false)
        {
            var pages = (await state.Engine.DatabaseManager.PageRepository.GetPageInfoByTags(pageTags)).Take(top).OrderBy(o => o.Name).ToList();
            var html = new StringBuilder();

            if (pages.Count > 0)
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

                    if (styleName == TwListStyle.Full)
                    {
                        if (page?.Description?.Length > 0)
                        {
                            html.Append(" - " + page.Description);
                        }
                    }

                    html.Append("</li>");
                }

                html.Append("</ul>");
            }

            return new TwPluginResult(html.ToString());
        }

        [TwStandardFunctionPlugin("SearchList", "Creates a list of pages by searching the page body for the specified text.")]
        public async Task<TwPluginResult> SearchList(ITwEngineState state,
            string searchPhrase, TwListStyle styleName = TwListStyle.Full, int pageSize = 5,
            bool pageSelector = true, bool allowFuzzyMatching = false, bool showNamespace = false)
        {
            string refTag = state.GetNextTagMarker("SearchList");
            int pageNumber = int.Parse(state.QueryString[refTag].ToString().DefaultWhenNullOrEmpty("1"));
            var searchTokens = searchPhrase.Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList();

            var pages = await state.Engine.DatabaseManager.PageRepository.PageSearchPaged(searchTokens, pageNumber, pageSize, allowFuzzyMatching);

            if (pages.Count == 0)
            {
                return new TwPluginResult(string.Empty);
            }

            var html = new StringBuilder();

            html.Append($"<div id=\"{refTag}\"></div>");

            if (styleName == TwListStyle.Full)
            {
                foreach (var page in pages)
                {
                    html.Append($"""<div class="border-bottom">""");

                    html.Append($"""<a href="{state.Engine.WikiConfiguration.BasePath}/{page.Navigation}" class="d-block text-reset py-2">""");
                    html.Append("""<span class="fw-semibold text-truncate">""");
                    html.Append(showNamespace ? page.Name : page.Title);
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
                    html.Append("</li>");
                }
                html.Append("</ul>");
            }

            if (pageSelector && (pageNumber > 1 || pages.Count > 0 && pages.First().PaginationPageCount > 1))
            {
                html.Append(TwPageSelectorGenerator.Generate(state.QueryString, pages.FirstOrDefault()?.PaginationPageCount ?? 1, refTag));
            }

            return new TwPluginResult(html.ToString())
            {
                Instructions = [TwResultInstruction.DisallowNestedProcessing]
            };
        }

        [TwStandardFunctionPlugin("NamespaceList", "Creates a list of pages by searching the page tags.")]
        public async Task<TwPluginResult> NamespaceList(ITwEngineState state,
            string[] namespaces, int top = 1000, TwListStyle styleName = TwListStyle.Full, bool showNamespace = false)
        {
            var pages = (await state.Engine.DatabaseManager.PageRepository.GetPageInfoByNamespaces(namespaces.ToList())).Take(top).OrderBy(o => o.Name).ToList();
            var html = new StringBuilder();

            if (pages.Count > 0)
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

                    if (styleName == TwListStyle.Full)
                    {
                        if (page?.Description?.Length > 0)
                        {
                            html.Append(" - " + page.Description);
                        }
                    }

                    html.Append("</li>");
                }

                html.Append("</ul>");
            }

            return new TwPluginResult(html.ToString());
        }

        [TwStandardFunctionPlugin("NamespaceGlossary", "Creates a glossary of pages in the specified namespace.")]
        public async Task<TwPluginResult> NamespaceGlossary(ITwEngineState state,
            string[] namespaces, int top = 1000, TwListStyle styleName = TwListStyle.Full, bool showNamespace = false)
        {
            string glossaryName = state.GetNextTagMarker("Glossary");
            var pages = (await state.Engine.DatabaseManager.PageRepository.GetPageInfoByNamespaces(namespaces.ToList())).Take(top).OrderBy(o => o.Name).ToList();
            var html = new StringBuilder();
            var alphabet = pages.Select(p => p.Title.Substring(0, 1).ToUpperInvariant()).Distinct();

            if (pages.Count > 0)
            {
                html.Append("<div class=\"text-center mb-2\">");
                foreach (var alpha in alphabet)
                {
                    html.Append(
                        $"<a href=\"#{glossaryName}_{alpha}\" class=\"mx-1 text-decoration-none\">{alpha}</a>");
                }
                html.Append("</div>");

                html.Append("<ul>");
                foreach (var alpha in alphabet)
                {
                    html.Append("<li><a name=\"" + glossaryName + "_" + alpha + "\">" + alpha + "</a></li>");

                    html.Append("<ul>");
                    foreach (var page in pages.Where(p => p.Title.StartsWith(alpha, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        if (showNamespace)
                        {
                            html.Append($"<li><a href=\"{state.Engine.WikiConfiguration.BasePath}/{page.Navigation}\">{page.Name}</a>");
                        }
                        else
                        {
                            html.Append($"<li><a href=\"{state.Engine.WikiConfiguration.BasePath}/{page.Navigation}\">{page.Title}</a>");
                        }

                        if (styleName == TwListStyle.Full)
                        {
                            if (page?.Description?.Length > 0)
                            {
                                html.Append(" - " + page.Description);
                            }
                        }
                        html.Append("</li>");
                    }
                    html.Append("</ul>");
                }

                html.Append("</ul>");
            }

            return new TwPluginResult(html.ToString());
        }

        [TwStandardFunctionPlugin("TagGlossary", "Creates a glossary of pages with the specified comma separated tags.")]
        public async Task<TwPluginResult> TagGlossary(ITwEngineState state,
            string[] pageTags, int top = 1000, TwListStyle styleName = TwListStyle.Full, bool showNamespace = false)
        {
            string glossaryName = state.GetNextTagMarker("Glossary");

            var pages = (await state.Engine.DatabaseManager.PageRepository.GetPageInfoByTags(pageTags)).Take(top).OrderBy(o => o.Name).ToList();
            var html = new StringBuilder();
            var alphabet = pages.Select(p => p.Title.Substring(0, 1).ToUpperInvariant()).Distinct();

            if (pages.Count > 0)
            {
                html.Append("<center>");
                foreach (var alpha in alphabet)
                {
                    html.Append("<a href=\"#" + glossaryName + "_" + alpha + "\">" + alpha + "</a>&nbsp;");
                }
                html.Append("</center>");

                html.Append("<ul>");
                foreach (var alpha in alphabet)
                {
                    html.Append("<li><a name=\"" + glossaryName + "_" + alpha + "\">" + alpha + "</a></li>");

                    html.Append("<ul>");
                    foreach (var page in pages.Where(p => p.Title.StartsWith(alpha, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        if (showNamespace)
                        {
                            html.Append($"<li><a href=\"{state.Engine.WikiConfiguration.BasePath}/{page.Navigation}\">{page.Name}</a>");
                        }
                        else
                        {
                            html.Append($"<li><a href=\"{state.Engine.WikiConfiguration.BasePath}/{page.Navigation}\">{page.Title}</a>");
                        }

                        if (styleName == TwListStyle.Full)
                        {
                            if (page?.Description?.Length > 0)
                            {
                                html.Append(" - " + page.Description);
                            }
                        }
                        html.Append("</li>");
                    }
                    html.Append("</ul>");
                }

                html.Append("</ul>");
            }

            return new TwPluginResult(html.ToString());
        }

        [TwStandardFunctionPlugin("TextGlossary", "Creates a glossary by searching page's body text for the specified comma separated list of words.")]
        public async Task<TwPluginResult> TextGlossary(ITwEngineState state,
            string searchPhrase, int top = 1000, TwListStyle styleName = TwListStyle.Full, bool showNamespace = false)
        {
            string glossaryName = state.GetNextTagMarker("Glossary");
            var searchTokens = searchPhrase.Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList();

            var pages = (await state.Engine.DatabaseManager.PageRepository.PageSearch(searchTokens)).Take(top).OrderBy(o => o.Name).ToList();
            var html = new StringBuilder();
            var alphabet = pages.Select(p => p.Title.Substring(0, 1).ToUpperInvariant()).Distinct();

            if (pages.Count > 0)
            {
                html.Append("<center>");
                foreach (var alpha in alphabet)
                {
                    html.Append("<a href=\"#" + glossaryName + "_" + alpha + "\">" + alpha + "</a>&nbsp;");
                }
                html.Append("</center>");

                html.Append("<ul>");
                foreach (var alpha in alphabet)
                {
                    html.Append("<li><a name=\"" + glossaryName + "_" + alpha + "\">" + alpha + "</a></li>");

                    html.Append("<ul>");
                    foreach (var page in pages.Where(p => p.Title.StartsWith(alpha, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        if (showNamespace)
                        {
                            html.Append($"<li><a href=\"{state.Engine.WikiConfiguration.BasePath}/{page.Navigation}\">{page.Name}</a>");
                        }
                        else
                        {
                            html.Append($"<li><a href=\"{state.Engine.WikiConfiguration.BasePath}/{page.Navigation}\">{page.Title}</a>");
                        }

                        if (styleName == TwListStyle.Full)
                        {
                            if (page?.Description?.Length > 0)
                            {
                                html.Append(" - " + page.Description);
                            }
                        }
                        html.Append("</li>");
                    }
                    html.Append("</ul>");
                }

                html.Append("</ul>");
            }

            return new TwPluginResult(html.ToString());
        }
    }
}
