using NTDLS.Helpers;
using System.Reflection;
using System.Text;
using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Attributes.Functions;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Library;
using TightWiki.Plugin.Models;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki.Plugin.Default
{
    [TwPlugin("Default Standard Functions", "Built-in standard functions.")]
    public class StandardFunctions
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

        [TwStandardFunctionPlugin("ProfileGlossary", "Creates a glossary of all user profiles.")]
        public async Task<TwPluginResult> ProfileGlossary(ITwEngineState state, int pageSize = 100, string? searchToken = null)
        {
            if (!state.Engine.WikiConfiguration.EnablePublicProfiles)
            {
                return new TwPluginResult("Public profiles are disabled.");
            }

            var html = new StringBuilder();
            string refTag = state.GetNextHttpQueryToken();
            int pageNumber = int.Parse(state.QueryString[refTag].ToString().DefaultWhenNullOrEmpty("1"));
            var profiles = await state.Engine.DatabaseManager.UsersRepository.GetAllPublicProfilesPaged(pageNumber, pageSize, searchToken);

            string glossaryName = "glossary_" + new Random().Next(0000).ToString();
            var alphabet = profiles.Select(p => p.AccountName.Substring(0, 1).ToUpperInvariant()).Distinct();

            if (profiles.Count > 0)
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
                    foreach (var profile in profiles.Where(p => p.AccountName.StartsWith(alpha, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        html.Append($"<li><a href=\"{state.Engine.WikiConfiguration.BasePath}/Profile/{profile.Navigation}/Public\">{profile.AccountName}</a>");
                        html.Append("</li>");
                    }
                    html.Append("</ul>");
                }

                html.Append("</ul>");
            }
            return new TwPluginResult(html.ToString());
        }

        //Creates a list of all user profiles.
        [TwStandardFunctionPlugin("ProfileList", "Creates a list of all user profiles.")]
        public async Task<TwPluginResult> ProfileList(ITwEngineState state, int pageSize = 100, string? searchToken = null)
        {
            if (!state.Engine.WikiConfiguration.EnablePublicProfiles)
            {
                return new TwPluginResult("Public profiles are disabled.");
            }

            var html = new StringBuilder();
            string refTag = state.GetNextHttpQueryToken();
            int pageNumber = int.Parse(state.QueryString[refTag].ToString().DefaultWhenNullOrEmpty("1"));
            var profiles = await state.Engine.DatabaseManager.UsersRepository.GetAllPublicProfilesPaged(pageNumber, pageSize, searchToken);

            if (profiles.Count > 0)
            {
                html.Append("<ul>");

                foreach (var profile in profiles)
                {
                    html.Append($"<li><a href=\"{state.Engine.WikiConfiguration.BasePath}/Profile/{profile.Navigation}/Public\">{profile.AccountName}</a>");
                    html.Append("</li>");
                }

                html.Append("</ul>");
            }

            if (profiles.Count > 0 && profiles.First().PaginationPageCount > 1)
            {
                html.Append(TwPageSelectorGenerator.Generate(state.QueryString, profiles.First().PaginationPageCount, refTag));
            }

            return new TwPluginResult(html.ToString());
        }

        [TwStandardFunctionPlugin("Attachments", "Creates a list of all attachments for a page.")]
        public async Task<TwPluginResult> Attachments(ITwEngineState state,
            TwListStyle styleName = TwListStyle.Full, int pageSize = 5, bool pageSelector = true, string? pageName = null)
        {
            string refTag = state.GetNextHttpQueryToken();

            int pageNumber = int.Parse(state.QueryString[refTag].ToString().DefaultWhenNullOrEmpty("1"));

            var navigation = TwNamespaceNavigation.CleanAndValidate(pageName ?? state.Page.Navigation);
            var attachments = await state.Engine.DatabaseManager.PageRepository.GetPageFilesInfoByPageNavigationAndPageRevisionPaged(navigation, pageNumber, pageSize, state.Revision);
            var html = new StringBuilder();

            if (attachments.Count > 0)
            {
                html.Append("<ul>");
                foreach (var file in attachments)
                {
                    if (state.Revision != null)
                    {
                        html.Append($"<li><a href=\"{state.Engine.WikiConfiguration.BasePath}/Page/Binary/{state.Page.Navigation}/{file.FileNavigation}/{state.Revision}\">{file.Name}</a>");
                    }
                    else
                    {
                        html.Append($"<li><a href=\"{state.Engine.WikiConfiguration.BasePath}/Page/Binary/{state.Page.Navigation}/{file.FileNavigation}\">{file.Name} </a>");
                    }

                    if (styleName == TwListStyle.Full)
                    {
                        html.Append($" - ({file.FriendlySize})");
                    }

                    html.Append("</li>");
                }
                html.Append("</ul>");

                if (pageSelector && attachments.Count > 0 && attachments.First().PaginationPageCount > 1)
                {
                    html.Append(TwPageSelectorGenerator.Generate(state.QueryString, attachments.First().PaginationPageCount, refTag));
                }
            }

            return new TwPluginResult(html.ToString());
        }

        [TwStandardFunctionPlugin("Revisions", "Creates a list of all revisions for a page.")]
        public async Task<TwPluginResult> Revisions(ITwEngineState state,
            TwListStyle styleName = TwListStyle.Full, int pageSize = 5, bool pageSelector = true, string? pageName = null)
        {
            if (state.Session == null)
            {
                throw new Exception($"Localization is not supported without SessionState.");
            }

            string refTag = state.GetNextHttpQueryToken();

            int pageNumber = int.Parse(state.QueryString[refTag].ToString().DefaultWhenNullOrEmpty("1"));

            var navigation = TwNamespaceNavigation.CleanAndValidate(pageName ?? state.Page.Navigation);
            var revisions = await state.Engine.DatabaseManager.PageRepository.GetPageRevisionsInfoByNavigationPaged(navigation, pageNumber, null, null, pageSize);
            var html = new StringBuilder();

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

        [TwStandardFunctionPlugin("Seq", "Inserts a sequence into the document.")]
        public async Task<TwPluginResult> Seq(ITwEngineState state, string key = "Default")
        {
            var sequences = state.GetStateValue("_sequences", new Dictionary<string, int>());

            if (sequences.ContainsKey(key) == false)
            {
                sequences.Add(key, 0);
            }

            sequences[key]++;

            return new TwPluginResult(sequences[key].ToString())
            {
                Instructions = [TwResultInstruction.OnlyReplaceFirstMatch]
            };
        }

        [TwStandardFunctionPlugin("EditLink", "Creates an edit link for the current page.")]
        public async Task<TwPluginResult> EditLink(ITwEngineState state, string linkText = "edit")
        {
            return new TwPluginResult($"<a href=\"{state.Engine.WikiConfiguration.BasePath}"
                + TwNamespaceNavigation.CleanAndValidate($"/{state.Page.Navigation}/Edit") + $"\">{linkText}</a>");
        }

        [TwStandardFunctionPlugin("Inject", "Injects an un-processed wiki body into the calling page.", isFirstChance: true)]
        public async Task<TwPluginResult> Inject(ITwEngineState state, string pageName)
        {

            var page = await GetPageFromNavigation(state.Engine.DatabaseManager.PageRepository, pageName);
            if (page != null)
            {
                return new TwPluginResult(page.Body)
                {
                    Instructions = [TwResultInstruction.TruncateTrailingLine]
                };
            }
            throw new Exception($"The include page was not found: [{pageName}]");

        }

        [TwStandardFunctionPlugin("Include", "Includes a processed wiki body into the calling page.", isFirstChance: true)]
        public async Task<TwPluginResult> include(ITwEngineState state, string pageName)
        {
            var page = await GetPageFromNavigation(state.Engine.DatabaseManager.PageRepository, pageName);
            if (page != null)
            {
                var childState = await state.TransformChild(page);

                MergeUserVariables(ref state, childState.Variables);
                MergeSnippets(ref state, childState.Snippets);

                return new TwPluginResult(childState.HtmlResult)
                {
                    Instructions = [TwResultInstruction.TruncateTrailingLine]
                };
            }
            throw new Exception($"The include page was not found: [{pageName}]");
        }

        [TwStandardFunctionPlugin("Set", "Sets a wiki variable.")]
        public async Task<TwPluginResult> Set(ITwEngineState state, string key, string value)
        {
            if (!state.Variables.TryAdd(key, value))
            {
                state.Variables[key] = value;
            }

            return new TwPluginResult(string.Empty)
            {
                Instructions = [TwResultInstruction.TruncateTrailingLine]
            };
        }

        [TwStandardFunctionPlugin("Get", "Gets a wiki variable.")]
        public async Task<TwPluginResult> Get(ITwEngineState state, string key)
        {
            if (state.Variables.TryGetValue(key, out var variable))
            {
                return new TwPluginResult(variable);
            }

            throw new Exception($"The wiki variable {key} is not defined. It should be set with ##Set() before calling Get().");
        }

        [TwStandardFunctionPlugin("Color", "Applies a color to the given text.")]
        public async Task<TwPluginResult> color(ITwEngineState state, string color, string text)
        {
            return new TwPluginResult($"<font color=\"{color}\">{text}</font>");
        }

        [TwStandardFunctionPlugin("Image", "Displays an image that is attached to the page.")]
        public async Task<TwPluginResult> Image(ITwEngineState state,
            string name, int? scale = null, string? altText = null, string? @class = null, int? maxWidth = null)
        {
            altText ??= name;
            @class ??= "img-fluid";

            bool explicitNamespace = name.Contains("::");
            bool isPageForeignImage = false;

            if (@class != null)
            {
                @class = $"class=\"{@class}\"";
            }
            else
            {
                @class = "class=\"img-fluid\"";
            }

            string navigation = state.Page.Navigation;
            if (name.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
            {
                string image = $"<a href=\"{name}\" target=\"_blank\"><img src=\"{name}\" border=\"0\" alt=\"{altText}\" {@class} /></a>";
                return new TwPluginResult(image);
            }
            else if (name.Contains('/'))
            {
                //Allow loading attached images from other pages.
                int slashIndex = name.IndexOf('/');
                navigation = TwNamespaceNavigation.CleanAndValidate(name.Substring(0, slashIndex));
                name = name.Substring(slashIndex + 1);
                isPageForeignImage = true;
            }

            if (explicitNamespace == false && state.Page.Namespace != null)
            {
                if (state.Engine.DatabaseManager.PageRepository.GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation(navigation, TwNamespaceNavigation.CleanAndValidate(name), state.Revision) == null)
                {
                    //If the image does not exist, and no namespace was specified, but the page has a namespace - then default to the pages namespace.
                    navigation = TwNamespaceNavigation.CleanAndValidate($"{state.Page.Namespace}::{name}");
                }
            }

            var queryParams = new List<string>();
            if (scale != 100) queryParams.Add($"Scale={scale}");
            if (maxWidth != null) queryParams.Add($"MaxWidth={maxWidth}");

            if (state.Revision != null && isPageForeignImage == false)
            {
                //Check for isPageForeignImage because we don't version foreign page files.
                string link = $"/Page/Image/{navigation}/{TwNamespaceNavigation.CleanAndValidate(name)}/{state.Revision}";
                string image = $"<a href=\"{state.Engine.WikiConfiguration.BasePath}{link}\" target=\"_blank\"><img src=\"{state.Engine.WikiConfiguration.BasePath}{link}?{string.Join('&', queryParams)}\" border=\"0\" alt=\"{altText}\" {@class} /></a>";
                return new TwPluginResult(image);
            }
            else
            {
                string link = $"/Page/Image/{navigation}/{TwNamespaceNavigation.CleanAndValidate(name)}";
                string image = $"<a href=\"{state.Engine.WikiConfiguration.BasePath}{link}\" target=\"_blank\"><img src=\"{state.Engine.WikiConfiguration.BasePath}{link}?{string.Join('&', queryParams)}\" border=\"0\" alt=\"{altText}\" {@class} /></a>";
                return new TwPluginResult(image);
            }
        }

        [TwStandardFunctionPlugin("File", "Displays a file download link.")]
        public async Task<TwPluginResult> File(ITwEngineState state, string name, string linkText, bool showSize = false)
        {
            bool explicitNamespace = name.Contains("::");
            bool isPageForeignFile = false;

            string navigation = state.Page.Navigation;
            if (name.Contains('/'))
            {
                //Allow loading attached images from other pages.
                int slashIndex = name.IndexOf("/");
                navigation = TwNamespaceNavigation.CleanAndValidate(name.Substring(0, slashIndex));
                name = name.Substring(slashIndex + 1);
                isPageForeignFile = true;
            }

            if (explicitNamespace == false && state.Page.Namespace != null)
            {
                if (state.Engine.DatabaseManager.PageRepository.GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation(navigation, TwNamespaceNavigation.CleanAndValidate(name), state.Revision) == null)
                {
                    //If the image does not exist, and no namespace was specified, but the page has a namespace - then default to the pages namespace.
                    navigation = TwNamespaceNavigation.CleanAndValidate($"{state.Page.Namespace}::{name}");
                }
            }

            var attachment = await state.Engine.DatabaseManager.PageRepository.GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation(navigation, TwNamespaceNavigation.CleanAndValidate(name), state.Revision);
            if (attachment != null)
            {
                string alt = linkText ?? name;

                if (showSize)
                {
                    alt += $" ({attachment.FriendlySize})";
                }

                if (state.Revision != null && isPageForeignFile == false)
                {
                    //Check for isPageForeignImage because we don't version foreign page files.
                    string link = $"/Page/Binary/{navigation}/{TwNamespaceNavigation.CleanAndValidate(name)}/{state.Revision}";
                    string image = $"<a href=\"{state.Engine.WikiConfiguration.BasePath}{link}\">{alt}</a>";
                    return new TwPluginResult(image);
                }
                else
                {
                    string link = $"/Page/Binary/{navigation}/{TwNamespaceNavigation.CleanAndValidate(name)}";
                    string image = $"<a href=\"{state.Engine.WikiConfiguration.BasePath}{link}\">{alt}</a>";
                    return new TwPluginResult(image);
                }
            }
            throw new Exception($"File not found [{name}]");
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

        [TwStandardFunctionPlugin("NamespaceGlossary", "Creates a glossary of pages in the specified namespace.")]
        public async Task<TwPluginResult> NamespaceGlossary(ITwEngineState state,
            string[] namespaces, int top = 1000, TwListStyle styleName = TwListStyle.Full, bool showNamespace = false)
        {
            string glossaryName = "glossary_" + new Random().Next(0, 1000000).ToString();
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

        [TwStandardFunctionPlugin("TagGlossary", "Creates a glossary of pages with the specified comma separated tags.")]
        public async Task<TwPluginResult> TagGlossary(ITwEngineState state,
            string[] pageTags, int top = 1000, TwListStyle styleName = TwListStyle.Full, bool showNamespace = false)
        {
            string glossaryName = "glossary_" + new Random().Next(0, 1000000).ToString();

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
            string glossaryName = "glossary_" + new Random().Next(0, 1000000).ToString();
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

        [TwStandardFunctionPlugin("SearchList", "Creates a list of pages by searching the page body for the specified text.")]
        public async Task<TwPluginResult> SearchList(ITwEngineState state,
            string searchPhrase, TwListStyle styleName = TwListStyle.Full, int pageSize = 5,
            bool pageSelector = true, bool allowFuzzyMatching = false, bool showNamespace = false)
        {
            string refTag = state.GetNextHttpQueryToken();
            int pageNumber = int.Parse(state.QueryString[refTag].ToString().DefaultWhenNullOrEmpty("1"));
            var searchTokens = searchPhrase.Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList();

            var pages = await state.Engine.DatabaseManager.PageRepository.PageSearchPaged(searchTokens, pageNumber, pageSize, allowFuzzyMatching);

            if (pages.Count == 0)
            {
                return new TwPluginResult(string.Empty);
            }

            var html = new StringBuilder();

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

        [TwStandardFunctionPlugin("Similar", "Displays a list of other related pages based on tags.")]
        public async Task<TwPluginResult> Similar(ITwEngineState state,
            int similarity = 80, TwTabularStyle styleName = TwTabularStyle.Full, int pageSize = 10, bool pageSelector = true)
        {
            string refTag = state.GetNextHttpQueryToken();

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
            string refTag = state.GetNextHttpQueryToken();

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

        [TwStandardFunctionPlugin("LastModifiedBy", "Displays the name of the person last modified the current page.")]
        public async Task<TwPluginResult> LastModifiedBy(ITwEngineState state)
        {
            return new TwPluginResult(state.Page.ModifiedByUserName)
            {
                Instructions = [TwResultInstruction.DisallowNestedProcessing]
            };
        }

        [TwStandardFunctionPlugin("PageRevisionCount", "Displays the total number of revisions for the current page.")]
        public async Task<TwPluginResult> PageRevisionCount(ITwEngineState state)
        {
            return new TwPluginResult($"{state.Page.MostCurrentRevision:n0}")
            {
                Instructions = [TwResultInstruction.DisallowNestedProcessing]
            };
        }

        [TwStandardFunctionPlugin("CreatedBy", "Displays the name of the person who created the current page.")]
        public async Task<TwPluginResult> CreatedBy(ITwEngineState state)
        {
            return new TwPluginResult(state.Page.CreatedByUserName)
            {
                Instructions = [TwResultInstruction.DisallowNestedProcessing]
            };
        }

        [TwStandardFunctionPlugin("PageviewCount", "Displays the total number views for the current page.")]
        public async Task<TwPluginResult> PageviewCount(ITwEngineState state)
        {
            int totalPageCount = await state.Engine.DatabaseManager.StatisticsRepository.GetPageTotalViewCount(state.Page.Id);
            return new TwPluginResult($"{totalPageCount:n0}")
            {
                Instructions = [TwResultInstruction.DisallowNestedProcessing]
            };
        }

        [TwStandardFunctionPlugin("PageURL", "Displays the URL for the current page.")]
        public async Task<TwPluginResult> PageURL(ITwEngineState state, TwLinkStyle styleName = TwLinkStyle.Link)
        {

            var siteAddress = (await state.Engine.DatabaseManager.ConfigurationRepository.Get(TwConfigGroup.Basic, "Address", "http://localhost")).TrimEnd('/');
            var link = $"{siteAddress}/{state.Page.Navigation}";

            switch (styleName)
            {
                case TwLinkStyle.Text:
                    return new TwPluginResult(link)
                    {
                        Instructions = [TwResultInstruction.DisallowNestedProcessing]
                    };
                case TwLinkStyle.Link:
                    return new TwPluginResult($"<a href='{link}'>{siteAddress}/{state.Page.Name}</a>")
                    {
                        Instructions = [TwResultInstruction.DisallowNestedProcessing]
                    };
                case TwLinkStyle.LinkName:
                    return new TwPluginResult($"<a href='{link}'>{state.Page.Name}</a>")
                    {
                        Instructions = [TwResultInstruction.DisallowNestedProcessing]
                    };
            }

            return new TwPluginResult();
        }

        [TwStandardFunctionPlugin("PageId", "Displays the ID of the current page.")]
        public async Task<TwPluginResult> PageId(ITwEngineState state)
        {
            return new TwPluginResult($"{state.Page.Id}")
            {
                Instructions = [TwResultInstruction.DisallowNestedProcessing]
            };
        }

        [TwStandardFunctionPlugin("PageCommentCount", "Displays the total number of comments for the current page.")]
        public async Task<TwPluginResult> PageCommentCount(ITwEngineState state)
        {
            int totalCommentCount = await state.Engine.DatabaseManager.PageRepository.GetTotalPageCommentCount(state.Page.Id);
            return new TwPluginResult($"{totalCommentCount:n0}")
            {
                Instructions = [TwResultInstruction.DisallowNestedProcessing]
            };
        }

        [TwStandardFunctionPlugin("LastModified", "Displays the date and time that the current page was last modified.")]
        public async Task<TwPluginResult> LastModified(ITwEngineState state)
        {
            if (state.Session == null)
            {
                throw new Exception($"Localization is not supported without SessionState.");
            }

            if (state.Page.ModifiedDate != DateTime.MinValue)
            {
                var localized = state.Session.LocalizeDateTime(state.Page.ModifiedDate);
                return new TwPluginResult($"{localized.ToShortDateString()} {localized.ToShortTimeString()}")
                {
                    Instructions = [TwResultInstruction.DisallowNestedProcessing]
                };
            }

            return new TwPluginResult(string.Empty);
        }

        [TwStandardFunctionPlugin("Created", "Displays the date and time that the current page was created.")]
        public async Task<TwPluginResult> Created(ITwEngineState state)
        {
            if (state.Session == null)
            {
                throw new Exception($"Localization is not supported without SessionState.");
            }

            if (state.Page.CreatedDate != DateTime.MinValue)
            {
                var localized = state.Session.LocalizeDateTime(state.Page.CreatedDate);
                return new TwPluginResult($"{localized.ToShortDateString()} {localized.ToShortTimeString()}")
                {
                    Instructions = [TwResultInstruction.DisallowNestedProcessing]
                };
            }

            return new TwPluginResult(string.Empty);
        }

        [TwStandardFunctionPlugin("DotNetVersion", "Displays the .NET version that TightWiki is running on.")]
        public async Task<TwPluginResult> DotNetVersion(ITwEngineState state)
        {
            return new TwPluginResult(System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription)
            {
                Instructions = [TwResultInstruction.DisallowNestedProcessing]
            };
        }

        [TwStandardFunctionPlugin("AppVersion", "Displays the version of the wiki engine.")]
        public async Task<TwPluginResult> AppVersion(ITwEngineState state)
        {
            var version = string.Join('.', (Assembly.GetExecutingAssembly()
                .GetName().Version?.ToString() ?? "0.0.0.0").Split('.').Take(3)); //Major.Minor.Patch

            return new TwPluginResult(version)
            {
                Instructions = [TwResultInstruction.DisallowNestedProcessing]
            };
        }

        [TwStandardFunctionPlugin("Name", "Displays the title of the current page.")]
        public async Task<TwPluginResult> Name(ITwEngineState state)
        {
            return new TwPluginResult(state.Page.Title)
            {
                Instructions = [TwResultInstruction.DisallowNestedProcessing]
            };
        }

        [TwStandardFunctionPlugin("SiteName", "Displays the title of the site.")]
        public async Task<TwPluginResult> SiteName(ITwEngineState state)
        {
            return new TwPluginResult(state.Engine.WikiConfiguration.Name)
            {
                Instructions = [TwResultInstruction.DisallowNestedProcessing]
            };
        }

        [TwStandardFunctionPlugin("Title", "Displays the title of the current page in title form.")]
        public async Task<TwPluginResult> Title(ITwEngineState state)
        {
            return new TwPluginResult($"<h1>{state.Page.Title}</h1>")
            {
                Instructions = [TwResultInstruction.DisallowNestedProcessing]
            };
        }

        [TwStandardFunctionPlugin("Description", "Displays the description of the current page.")]
        public async Task<TwPluginResult> Description(ITwEngineState state)
        {
            return new TwPluginResult($"{state.Page.Description}")
            {
                Instructions = [TwResultInstruction.DisallowNestedProcessing]
            };
        }

        [TwStandardFunctionPlugin("Namespace", "Displays the namespace of the current page.")]
        public async Task<TwPluginResult> @Namespace(ITwEngineState state)
        {
            return new TwPluginResult(state.Page.Namespace ?? string.Empty)
            {
                Instructions = [TwResultInstruction.DisallowNestedProcessing]
            };
        }

        [TwStandardFunctionPlugin("Snippet", "Displays the value of a snippet.")]
        public async Task<TwPluginResult> Snippet(ITwEngineState state, string name)
        {
            if (state.Snippets.TryGetValue(name, out string? value))
            {
                return new TwPluginResult(value);
            }
            else
            {
                return new TwPluginResult(string.Empty);
            }
        }

        [TwStandardFunctionPlugin("BR", "Inserts a line break into the page.")]
        public async Task<TwPluginResult> BR(ITwEngineState state, int count = 1) => await NewLine(state, count);

        [TwStandardFunctionPlugin("NL", "Inserts a line break into the page.")]
        public async Task<TwPluginResult> NL(ITwEngineState state, int count = 1) => await NewLine(state, count);

        [TwStandardFunctionPlugin("NewLine", "Inserts a line break into the page.")]
        public async Task<TwPluginResult> NewLine(ITwEngineState state, int count = 1) //##NewLine([optional:default=1]count)
        {
            var lineBreaks = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                lineBreaks.Append("<br />");
            }
            return new TwPluginResult(lineBreaks.ToString());
        }

        [TwStandardFunctionPlugin("HR", "Inserts a horizontal rule into the page.")]
        public async Task<TwPluginResult> HR(ITwEngineState state, int height = 1)
        {
            return new TwPluginResult($"<hr class=\"my-{height}\">");
        }

        [TwStandardFunctionPlugin("Navigation", "Displays the navigation text for the current page.")]
        public async Task<TwPluginResult> Navigation(ITwEngineState state)
        {
            return new TwPluginResult(state.Page.Navigation);
        }

        [TwStandardFunctionPlugin("SystemEmojiList", "Displays a list of emojis for the specified category.")]
        public async Task<TwPluginResult> SystemEmojiList(ITwEngineState state)
        {
            StringBuilder html = new();

            html.Append($"<table class=\"table table-striped table-bordered \">");

            html.Append($"<thead>");
            html.Append($"<tr>");
            html.Append($"<td><strong>Name</strong></td>");
            html.Append($"<td><strong>Image</strong></td>");
            html.Append($"<td><strong>Shortcut</strong></td>");
            html.Append($"</tr>");
            html.Append($"</thead>");

            string category = state.QueryString["Category"].ToString();

            html.Append($"<tbody>");

            if (string.IsNullOrWhiteSpace(category) == false)
            {
                var emojis = await state.Engine.DatabaseManager.EmojiRepository.GetEmojisByCategory(category);

                foreach (var emoji in emojis)
                {
                    html.Append($"<tr>");
                    html.Append($"<td>{emoji.Name}</td>");
                    //html.Append($"<td><img src=\"/images/emoji/{emoji.Path}\" /></td>");
                    html.Append($"<td><img src=\"{state.Engine.WikiConfiguration.BasePath}/File/Emoji/{emoji.Name.ToLowerInvariant()}\" /></td>");
                    html.Append($"<td>{emoji.Shortcut}</td>");
                    html.Append($"</tr>");
                }
            }

            html.Append($"</tbody>");
            html.Append($"</table>");

            return new TwPluginResult(html.ToString())
            {
                Instructions = [TwResultInstruction.DisallowNestedProcessing]
            };
        }

        [TwStandardFunctionPlugin("SystemEmojiCategoryList", "Displays a list of emoji categories.")]
        public async Task<TwPluginResult> SystemEmojiCategoryList(ITwEngineState state)
        {
            var categories = await state.Engine.DatabaseManager.EmojiRepository.GetEmojiCategoriesGrouped();

            StringBuilder html = new();

            html.Append($"<table class=\"table table-striped table-bordered \">");

            int rowNumber = 0;

            html.Append($"<thead>");
            html.Append($"<tr>");
            html.Append($"<td><strong>Name</strong></td>");
            html.Append($"<td><strong>Count of Emojis</strong></td>");
            html.Append($"</tr>");
            html.Append($"</thead>");

            foreach (var category in categories)
            {
                if (rowNumber == 1)
                {
                    html.Append($"<tbody>");
                }

                html.Append($"<tr>");
                html.Append($"<td><a href=\"{state.Engine.WikiConfiguration.BasePath}/wiki_help::list_of_emojis_by_category?category={category.Category}\">{category.Category}</a></td>");
                html.Append($"<td>{category.EmojiCount:N0}</td>");
                html.Append($"</tr>");
                rowNumber++;
            }

            html.Append($"</tbody>");
            html.Append($"</table>");

            return new TwPluginResult(html.ToString())
            {
                Instructions = [TwResultInstruction.DisallowNestedProcessing]
            };
        }
    }
}
