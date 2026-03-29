using NTDLS.Helpers;
using System.Reflection;
using System.Text;
using TightWiki.Library;
using TightWiki.Models.DataModels;
using TightWiki.Plugin;
using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Interfaces;
using TightWiki.Repository;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki.Engine.Implementation.Functions
{
    [TwFunctionModule("Standard Functions", "Built-in standard functions.")]
    public class StandardFunctions
        : ITwFunctionModule
    {
        #region Helpers.

        private static async Task<WikiPage?> GetPageFromNavigation(string routeData)
        {
            routeData = TwNamespaceNavigation.CleanAndValidate(routeData);
            var page = await PageRepository.GetPageRevisionByNavigation(routeData);
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

        [TwStandardFunction("ProfileGlossary", "Creates a glossary of all user profiles.")]
        public async Task<TwHandlerResult> ProfileGlossary(ITwEngineState state, int pageSize = 100, string? searchToken = null)
        {
            if (!state.Engine.WikiConfiguration.EnablePublicProfiles)
            {
                return new TwHandlerResult("Public profiles are disabled.");
            }

            var html = new StringBuilder();
            string refTag = state.GetNextHttpQueryToken();
            int pageNumber = int.Parse(state.QueryString[refTag].ToString().DefaultWhenNullOrEmpty("1"));
            var profiles = await UsersRepository.GetAllPublicProfilesPaged(pageNumber, pageSize, searchToken);

            string glossaryName = "glossary_" + new Random().Next(0, 1000000).ToString();
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
            return new TwHandlerResult(html.ToString());
        }

        //Creates a list of all user profiles.
        [TwStandardFunction("ProfileList", "Creates a list of all user profiles.")]
        public async Task<TwHandlerResult> ProfileList(ITwEngineState state, int pageSize = 100, string? searchToken = null)
        {
            if (!state.Engine.WikiConfiguration.EnablePublicProfiles)
            {
                return new TwHandlerResult("Public profiles are disabled.");
            }

            var html = new StringBuilder();
            string refTag = state.GetNextHttpQueryToken();
            int pageNumber = int.Parse(state.QueryString[refTag].ToString().DefaultWhenNullOrEmpty("1"));
            var profiles = await UsersRepository.GetAllPublicProfilesPaged(pageNumber, pageSize, searchToken);

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
                html.Append(PageSelectorGenerator.Generate(state.QueryString, profiles.First().PaginationPageCount, refTag));
            }

            return new TwHandlerResult(html.ToString());
        }

        [TwStandardFunction("Attachments", "Creates a list of all attachments for a page.")]
        public async Task<TwHandlerResult> Attachments(ITwEngineState state,
            TightWikiListStyle styleName = TightWikiListStyle.Full, int pageSize = 5, bool pageSelector = true, string? pageName = null)
        {
            string refTag = state.GetNextHttpQueryToken();

            int pageNumber = int.Parse(state.QueryString[refTag].ToString().DefaultWhenNullOrEmpty("1"));

            var navigation = TwNamespaceNavigation.CleanAndValidate(pageName ?? state.Page.Navigation);
            var attachments = await PageFileRepository.GetPageFilesInfoByPageNavigationAndPageRevisionPaged(navigation, pageNumber, pageSize, state.Revision);
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

                    if (styleName == TightWikiListStyle.Full)
                    {
                        html.Append($" - ({file.FriendlySize})");
                    }

                    html.Append("</li>");
                }
                html.Append("</ul>");

                if (pageSelector && attachments.Count > 0 && attachments.First().PaginationPageCount > 1)
                {
                    html.Append(PageSelectorGenerator.Generate(state.QueryString, attachments.First().PaginationPageCount, refTag));
                }
            }

            return new TwHandlerResult(html.ToString());
        }

        [TwStandardFunction("Revisions", "Creates a list of all revisions for a page.")]
        public async Task<TwHandlerResult> Revisions(ITwEngineState state,
            TightWikiListStyle styleName = TightWikiListStyle.Full, int pageSize = 5, bool pageSelector = true, string? pageName = null)
        {
            if (state.Session == null)
            {
                throw new Exception($"Localization is not supported without SessionState.");
            }

            string refTag = state.GetNextHttpQueryToken();

            int pageNumber = int.Parse(state.QueryString[refTag].ToString().DefaultWhenNullOrEmpty("1"));

            var navigation = TwNamespaceNavigation.CleanAndValidate(pageName ?? state.Page.Navigation);
            var revisions = await PageRepository.GetPageRevisionsInfoByNavigationPaged(navigation, pageNumber, null, null, pageSize);
            var html = new StringBuilder();

            if (revisions.Count > 0)
            {
                html.Append("<ul>");
                foreach (var item in revisions)
                {
                    html.Append($"<li><a href=\"{state.Engine.WikiConfiguration.BasePath}/{item.Navigation}/{item.Revision}\">{item.Revision} by {item.ModifiedByUserName} on {state.Session.LocalizeDateTime(item.ModifiedDate)}</a>");

                    if (styleName == TightWikiListStyle.Full)
                    {
                        var thisRev = await PageRepository.GetPageRevisionByNavigation(state.Page.Navigation, item.Revision);
                        var prevRev = await PageRepository.GetPageRevisionByNavigation(state.Page.Navigation, item.Revision - 1);

                        var summaryText = Differentiator.GetComparisonSummary(thisRev?.Body ?? string.Empty, prevRev?.Body ?? string.Empty);

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
                    html.Append(PageSelectorGenerator.Generate(state.QueryString, revisions.First().PaginationPageCount, refTag));
                }
            }

            return new TwHandlerResult(html.ToString());
        }

        [TwStandardFunction("Seq", "Inserts a sequence into the document.")]
        public async Task<TwHandlerResult> Seq(ITwEngineState state, string key = "Default")
        {
            var sequences = state.GetStateValue("_sequences", new Dictionary<string, int>());

            if (sequences.ContainsKey(key) == false)
            {
                sequences.Add(key, 0);
            }

            sequences[key]++;

            return new TwHandlerResult(sequences[key].ToString())
            {
                Instructions = [HandlerResultInstruction.OnlyReplaceFirstMatch]
            };
        }

        [TwStandardFunction("EditLink", "Creates an edit link for the current page.")]
        public async Task<TwHandlerResult> EditLink(ITwEngineState state, string linkText = "edit")
        {
            return new TwHandlerResult($"<a href=\"{state.Engine.WikiConfiguration.BasePath}"
                + TwNamespaceNavigation.CleanAndValidate($"/{state.Page.Navigation}/Edit") + $"\">{linkText}</a>");
        }

        [TwStandardFunction("Inject", "Injects an un-processed wiki body into the calling page.", true)]
        public async Task<TwHandlerResult> Inject(ITwEngineState state, string pageName)
        {

            var page = await GetPageFromNavigation(pageName);
            if (page != null)
            {
                return new TwHandlerResult(page.Body)
                {
                    Instructions = [HandlerResultInstruction.TruncateTrailingLine]
                };
            }
            throw new Exception($"The include page was not found: [{pageName}]");

        }

        [TwStandardFunction("Include", "Includes a processed wiki body into the calling page.", true)]
        public async Task<TwHandlerResult> include(ITwEngineState state, string pageName)
        {
            var page = await GetPageFromNavigation(pageName);
            if (page != null)
            {
                var childState = await state.TransformChild(page);

                MergeUserVariables(ref state, childState.Variables);
                MergeSnippets(ref state, childState.Snippets);

                return new TwHandlerResult(childState.HtmlResult)
                {
                    Instructions = [HandlerResultInstruction.TruncateTrailingLine]
                };
            }
            throw new Exception($"The include page was not found: [{pageName}]");
        }

        [TwStandardFunction("Set", "Sets a wiki variable.")]
        public async Task<TwHandlerResult> Set(ITwEngineState state, string key, string value)
        {
            if (!state.Variables.TryAdd(key, value))
            {
                state.Variables[key] = value;
            }

            return new TwHandlerResult(string.Empty)
            {
                Instructions = [HandlerResultInstruction.TruncateTrailingLine]
            };
        }

        [TwStandardFunction("Get", "Gets a wiki variable.")]
        public async Task<TwHandlerResult> Get(ITwEngineState state, string key)
        {
            if (state.Variables.TryGetValue(key, out var variable))
            {
                return new TwHandlerResult(variable);
            }

            throw new Exception($"The wiki variable {key} is not defined. It should be set with ##Set() before calling Get().");
        }

        [TwStandardFunction("Color", "Applies a color to the given text.")]
        public async Task<TwHandlerResult> color(ITwEngineState state, string color, string text)
        {
            return new TwHandlerResult($"<font color=\"{color}\">{text}</font>");
        }

        [TwStandardFunction("Tag", "Associates tags with a page. These are saved with the page and can also be displayed.")]
        public async Task<TwHandlerResult> Tag(ITwEngineState state, params string[] tags) //##tag(pipe|separated|list|of|tags)
        {
            state.Tags.AddRange(tags);
            state.Tags = state.Tags.Distinct().ToList();

            return new TwHandlerResult(string.Empty)
            {
                Instructions = [HandlerResultInstruction.TruncateTrailingLine]
            };
        }

        [TwStandardFunction("Image", "Displays an image that is attached to the page.")]
        public async Task<TwHandlerResult> Image(ITwEngineState state,
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
                return new TwHandlerResult(image);
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
                if (PageFileRepository.GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation(navigation, TwNamespaceNavigation.CleanAndValidate(name), state.Revision) == null)
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
                return new TwHandlerResult(image);
            }
            else
            {
                string link = $"/Page/Image/{navigation}/{TwNamespaceNavigation.CleanAndValidate(name)}";
                string image = $"<a href=\"{state.Engine.WikiConfiguration.BasePath}{link}\" target=\"_blank\"><img src=\"{state.Engine.WikiConfiguration.BasePath}{link}?{string.Join('&', queryParams)}\" border=\"0\" alt=\"{altText}\" {@class} /></a>";
                return new TwHandlerResult(image);
            }
        }

        [TwStandardFunction("File", "Displays a file download link.")]
        public async Task<TwHandlerResult> File(ITwEngineState state, string name, string linkText, bool showSize = false)
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
                if (PageFileRepository.GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation(navigation, TwNamespaceNavigation.CleanAndValidate(name), state.Revision) == null)
                {
                    //If the image does not exist, and no namespace was specified, but the page has a namespace - then default to the pages namespace.
                    navigation = TwNamespaceNavigation.CleanAndValidate($"{state.Page.Namespace}::{name}");
                }
            }

            var attachment = await PageFileRepository.GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation(navigation, TwNamespaceNavigation.CleanAndValidate(name), state.Revision);
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
                    return new TwHandlerResult(image);
                }
                else
                {
                    string link = $"/Page/Binary/{navigation}/{TwNamespaceNavigation.CleanAndValidate(name)}";
                    string image = $"<a href=\"{state.Engine.WikiConfiguration.BasePath}{link}\">{alt}</a>";
                    return new TwHandlerResult(image);
                }
            }
            throw new Exception($"File not found [{name}]");
        }

        [TwStandardFunction("RecentlyModified", "Creates a list of pages that have been recently modified.")]
        public async Task<TwHandlerResult> RecentlyModified(ITwEngineState state,
            int top = 1000, TightWikiListStyle styleName = TightWikiListStyle.Full, bool showNamespace = false)
        {
            var pages = (await PageRepository.GetTopRecentlyModifiedPagesInfo(top))
                .OrderByDescending(o => o.ModifiedDate).ThenBy(o => o.Title).ToList();

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

                    if (styleName == TightWikiListStyle.Full)
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

            return new TwHandlerResult(html.ToString());
        }

        [TwStandardFunction("NamespaceGlossary", "Creates a glossary of pages in the specified namespace.")]
        public async Task<TwHandlerResult> NamespaceGlossary(ITwEngineState state,
            string[] namespaces, int top = 1000, TightWikiListStyle styleName = TightWikiListStyle.Full, bool showNamespace = false)
        {
            string glossaryName = "glossary_" + new Random().Next(0, 1000000).ToString();
            var pages = (await PageRepository.GetPageInfoByNamespaces(namespaces.ToList())).Take(top).OrderBy(o => o.Name).ToList();
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

                        if (styleName == TightWikiListStyle.Full)
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

            return new TwHandlerResult(html.ToString());
        }

        [TwStandardFunction("NamespaceList", "Creates a list of pages by searching the page tags.")]
        public async Task<TwHandlerResult> NamespaceList(ITwEngineState state,
            string[] namespaces, int top = 1000, TightWikiListStyle styleName = TightWikiListStyle.Full, bool showNamespace = false)
        {

            var pages = (await PageRepository.GetPageInfoByNamespaces(namespaces.ToList())).Take(top).OrderBy(o => o.Name).ToList();
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

                    if (styleName == TightWikiListStyle.Full)
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

            return new TwHandlerResult(html.ToString());
        }

        [TwStandardFunction("TagGlossary", "Creates a glossary of pages with the specified comma separated tags.")]
        public async Task<TwHandlerResult> TagGlossary(ITwEngineState state,
            string[] pageTags, int top = 1000, TightWikiListStyle styleName = TightWikiListStyle.Full, bool showNamespace = false)
        {
            string glossaryName = "glossary_" + new Random().Next(0, 1000000).ToString();

            var pages = (await PageRepository.GetPageInfoByTags(pageTags)).Take(top).OrderBy(o => o.Name).ToList();
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

                        if (styleName == TightWikiListStyle.Full)
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

            return new TwHandlerResult(html.ToString());
        }

        [TwStandardFunction("TextGlossary", "Creates a glossary by searching page's body text for the specified comma separated list of words.")]
        public async Task<TwHandlerResult> TextGlossary(ITwEngineState state,
            string searchPhrase, int top = 1000, TightWikiListStyle styleName = TightWikiListStyle.Full, bool showNamespace = false)
        {
            string glossaryName = "glossary_" + new Random().Next(0, 1000000).ToString();
            var searchTokens = searchPhrase.Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList();

            var pages = (await PageRepository.PageSearch(searchTokens)).Take(top).OrderBy(o => o.Name).ToList();
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

                        if (styleName == TightWikiListStyle.Full)
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

            return new TwHandlerResult(html.ToString());
        }

        [TwStandardFunction("SearchList", "Creates a list of pages by searching the page body for the specified text.")]
        public async Task<TwHandlerResult> SearchList(ITwEngineState state,
            string searchPhrase, TightWikiListStyle styleName = TightWikiListStyle.Full, int pageSize = 5,
            bool pageSelector = true, bool allowFuzzyMatching = false, bool showNamespace = false)
        {
            string refTag = state.GetNextHttpQueryToken();
            int pageNumber = int.Parse(state.QueryString[refTag].ToString().DefaultWhenNullOrEmpty("1"));
            var searchTokens = searchPhrase.Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList();

            var pages = await PageRepository.PageSearchPaged(searchTokens, pageNumber, pageSize, allowFuzzyMatching);
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

                    if (styleName == TightWikiListStyle.Full)
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

            if (pageSelector && (pageNumber > 1 || pages.Count > 0 && pages.First().PaginationPageCount > 1))
            {
                html.Append(PageSelectorGenerator.Generate(state.QueryString, pages.FirstOrDefault()?.PaginationPageCount ?? 1, refTag));
            }

            return new TwHandlerResult(html.ToString());
        }

        [TwStandardFunction("TagList", "Creates a list of pages by searching the page tags.")]
        public async Task<TwHandlerResult> TagList(ITwEngineState state,
            string[] pageTags, int top = 1000, TightWikiListStyle styleName = TightWikiListStyle.Full, bool showNamespace = false)
        {
            var pages = (await PageRepository.GetPageInfoByTags(pageTags)).Take(top).OrderBy(o => o.Name).ToList();
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

                    if (styleName == TightWikiListStyle.Full)
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

            return new TwHandlerResult(html.ToString());
        }

        [TwStandardFunction("Similar", "Displays a list of other related pages based on tags.")]
        public async Task<TwHandlerResult> Similar(ITwEngineState state,
            int similarity = 80, TightWikiTabularStyle styleName = TightWikiTabularStyle.Full, int pageSize = 10, bool pageSelector = true)
        {
            string refTag = state.GetNextHttpQueryToken();

            int pageNumber = int.Parse(state.QueryString[refTag].ToString().DefaultWhenNullOrEmpty("1"));
            var html = new StringBuilder();

            var pages = await PageRepository.GetSimilarPagesPaged(state.Page.Id, similarity, pageNumber, pageSize);

            switch (styleName)
            {
                case TightWikiTabularStyle.List:
                    html.Append("<ul>");
                    foreach (var page in pages)
                    {
                        html.Append($"<li><a href=\"{state.Engine.WikiConfiguration.BasePath}/{page.Navigation}\">{page.Title}</a>");
                    }
                    html.Append("</ul>");
                    break;
                case TightWikiTabularStyle.Flat:
                    foreach (var page in pages)
                    {
                        if (html.Length > 0) html.Append(" | ");
                        html.Append($"<a href=\"{state.Engine.WikiConfiguration.BasePath}/{page.Navigation}\">{page.Title}</a>");
                    }
                    break;
                case TightWikiTabularStyle.Full:
                    html.Append("<ul>");
                    foreach (var page in pages)
                    {
                        html.Append($"<li><a href=\"{state.Engine.WikiConfiguration.BasePath}/{page.Navigation}\">{page.Title}</a> - {page.Description}");
                    }
                    html.Append("</ul>");
                    break;
            }

            if (pageSelector && pages.Count > 0 && pages.First().PaginationPageCount > 1)
            {
                html.Append(PageSelectorGenerator.Generate(state.QueryString, pages.First().PaginationPageCount, refTag));
            }

            return new TwHandlerResult(html.ToString());
        }

        [TwStandardFunction("Related", "Displays a list of other related pages based incoming links.")]
        public async Task<TwHandlerResult> Related(ITwEngineState state,
            TightWikiTabularStyle styleName = TightWikiTabularStyle.Full, int pageSize = 10, bool pageSelector = true)
        {
            string refTag = state.GetNextHttpQueryToken();

            int pageNumber = int.Parse(state.QueryString[refTag].ToString().DefaultWhenNullOrEmpty("1"));
            var html = new StringBuilder();

            var pages = await PageRepository.GetRelatedPagesPaged(state.Page.Id, pageNumber, pageSize);

            switch (styleName)
            {
                case TightWikiTabularStyle.List:
                    html.Append("<ul>");
                    foreach (var page in pages)
                    {
                        html.Append($"<li><a href=\"{state.Engine.WikiConfiguration.BasePath}/{page.Navigation}\">{page.Title}</a>");
                    }
                    html.Append("</ul>");
                    break;
                case TightWikiTabularStyle.Flat:
                    foreach (var page in pages)
                    {
                        if (html.Length > 0) html.Append(" | ");
                        html.Append($"<a href=\"{state.Engine.WikiConfiguration.BasePath}/{page.Navigation}\">{page.Title}</a>");
                    }
                    break;
                case TightWikiTabularStyle.Full:
                    html.Append("<ul>");
                    foreach (var page in pages)
                    {
                        html.Append($"<li><a href=\"{state.Engine.WikiConfiguration.BasePath}/{page.Navigation}\">{page.Title}</a> - {page.Description}");
                    }
                    html.Append("</ul>");
                    break;
            }

            if (pageSelector && pages.Count > 0 && pages.First().PaginationPageCount > 1)
            {
                html.Append(PageSelectorGenerator.Generate(state.QueryString, pages.First().PaginationPageCount, refTag));
            }

            return new TwHandlerResult(html.ToString());
        }

        [TwStandardFunction("LastModifiedBy", "Displays the name of the person last modified the current page.")]
        public async Task<TwHandlerResult> LastModifiedBy(ITwEngineState state)
        {
            return new TwHandlerResult(state.Page.ModifiedByUserName)
            {
                Instructions = [HandlerResultInstruction.DisallowNestedProcessing]
            };
        }

        [TwStandardFunction("PageRevisionCount", "Displays the total number of revisions for the current page.")]
        public async Task<TwHandlerResult> PageRevisionCount(ITwEngineState state)
        {
            return new TwHandlerResult($"{state.Page.MostCurrentRevision:n0}")
            {
                Instructions = [HandlerResultInstruction.DisallowNestedProcessing]
            };
        }

        [TwStandardFunction("CreatedBy", "Displays the name of the person who created the current page.")]
        public async Task<TwHandlerResult> CreatedBy(ITwEngineState state)
        {
            return new TwHandlerResult(state.Page.CreatedByUserName)
            {
                Instructions = [HandlerResultInstruction.DisallowNestedProcessing]
            };
        }

        [TwStandardFunction("PageviewCount", "Displays the total number views for the current page.")]
        public async Task<TwHandlerResult> PageviewCount(ITwEngineState state)
        {
            int totalPageCount = await StatisticsRepository.GetPageTotalViewCount(state.Page.Id);
            return new TwHandlerResult($"{totalPageCount:n0}")
            {
                Instructions = [HandlerResultInstruction.DisallowNestedProcessing]
            };
        }

        [TwStandardFunction("PageURL", "Displays the URL for the current page.")]
        public async Task<TwHandlerResult> PageURL(ITwEngineState state, TightWikiLinkStyle styleName = TightWikiLinkStyle.Link)
        {

            var siteAddress = (await ConfigurationRepository.Get(WikiConfigurationGroup.Basic, "Address", "http://localhost")).TrimEnd('/');
            var link = $"{siteAddress}/{state.Page.Navigation}";

            switch (styleName)
            {
                case TightWikiLinkStyle.Text:
                    return new TwHandlerResult(link)
                    {
                        Instructions = [HandlerResultInstruction.DisallowNestedProcessing]
                    };
                case TightWikiLinkStyle.Link:
                    return new TwHandlerResult($"<a href='{link}'>{siteAddress}/{state.Page.Name}</a>")
                    {
                        Instructions = [HandlerResultInstruction.DisallowNestedProcessing]
                    };
                case TightWikiLinkStyle.LinkName:
                    return new TwHandlerResult($"<a href='{link}'>{state.Page.Name}</a>")
                    {
                        Instructions = [HandlerResultInstruction.DisallowNestedProcessing]
                    };
            }

            return new TwHandlerResult();
        }

        [TwStandardFunction("PageId", "Displays the ID of the current page.")]
        public async Task<TwHandlerResult> PageId(ITwEngineState state)
        {
            return new TwHandlerResult($"{state.Page.Id}")
            {
                Instructions = [HandlerResultInstruction.DisallowNestedProcessing]
            };
        }

        [TwStandardFunction("PageCommentCount", "Displays the total number of comments for the current page.")]
        public async Task<TwHandlerResult> PageCommentCount(ITwEngineState state)
        {
            int totalCommentCount = await PageRepository.GetTotalPageCommentCount(state.Page.Id);
            return new TwHandlerResult($"{totalCommentCount:n0}")
            {
                Instructions = [HandlerResultInstruction.DisallowNestedProcessing]
            };
        }

        [TwStandardFunction("LastModified", "Displays the date and time that the current page was last modified.")]
        public async Task<TwHandlerResult> LastModified(ITwEngineState state)
        {
            if (state.Session == null)
            {
                throw new Exception($"Localization is not supported without SessionState.");
            }

            if (state.Page.ModifiedDate != DateTime.MinValue)
            {
                var localized = state.Session.LocalizeDateTime(state.Page.ModifiedDate);
                return new TwHandlerResult($"{localized.ToShortDateString()} {localized.ToShortTimeString()}")
                {
                    Instructions = [HandlerResultInstruction.DisallowNestedProcessing]
                };
            }

            return new TwHandlerResult(string.Empty);
        }

        [TwStandardFunction("Created", "Displays the date and time that the current page was created.")]
        public async Task<TwHandlerResult> Created(ITwEngineState state)
        {
            if (state.Session == null)
            {
                throw new Exception($"Localization is not supported without SessionState.");
            }

            if (state.Page.CreatedDate != DateTime.MinValue)
            {
                var localized = state.Session.LocalizeDateTime(state.Page.CreatedDate);
                return new TwHandlerResult($"{localized.ToShortDateString()} {localized.ToShortTimeString()}")
                {
                    Instructions = [HandlerResultInstruction.DisallowNestedProcessing]
                };
            }

            return new TwHandlerResult(string.Empty);
        }

        [TwStandardFunction("DotNetVersion", "Displays the .NET version that TightWiki is running on.")]
        public async Task<TwHandlerResult> DotNetVersion(ITwEngineState state)
        {
            return new TwHandlerResult(System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription)
            {
                Instructions = [HandlerResultInstruction.DisallowNestedProcessing]
            };
        }

        [TwStandardFunction("AppVersion", "Displays the version of the wiki engine.")]
        public async Task<TwHandlerResult> AppVersion(ITwEngineState state)
        {
            var version = string.Join('.', (Assembly.GetExecutingAssembly()
                .GetName().Version?.ToString() ?? "0.0.0.0").Split('.').Take(3)); //Major.Minor.Patch

            return new TwHandlerResult(version)
            {
                Instructions = [HandlerResultInstruction.DisallowNestedProcessing]
            };
        }

        [TwStandardFunction("Name", "Displays the title of the current page.")]
        public async Task<TwHandlerResult> Name(ITwEngineState state)
        {
            return new TwHandlerResult(state.Page.Title)
            {
                Instructions = [HandlerResultInstruction.DisallowNestedProcessing]
            };
        }

        [TwStandardFunction("SiteName", "Displays the title of the site.")]
        public async Task<TwHandlerResult> SiteName(ITwEngineState state)
        {
            return new TwHandlerResult(state.Engine.WikiConfiguration.Name)
            {
                Instructions = [HandlerResultInstruction.DisallowNestedProcessing]
            };
        }

        [TwStandardFunction("Title", "Displays the title of the current page in title form.")]
        public async Task<TwHandlerResult> Title(ITwEngineState state)
        {
            return new TwHandlerResult($"<h1>{state.Page.Title}</h1>")
            {
                Instructions = [HandlerResultInstruction.DisallowNestedProcessing]
            };
        }

        [TwStandardFunction("Description", "Displays the description of the current page.")]
        public async Task<TwHandlerResult> Description(ITwEngineState state)
        {
            return new TwHandlerResult($"{state.Page.Description}")
            {
                Instructions = [HandlerResultInstruction.DisallowNestedProcessing]
            };
        }

        [TwStandardFunction("Namespace", "Displays the namespace of the current page.")]
        public async Task<TwHandlerResult> @Namespace(ITwEngineState state)
        {
            return new TwHandlerResult(state.Page.Namespace ?? string.Empty)
            {
                Instructions = [HandlerResultInstruction.DisallowNestedProcessing]
            };
        }

        [TwStandardFunction("Snippet", "Displays the value of a snippet.")]
        public async Task<TwHandlerResult> Snippet(ITwEngineState state, string name)
        {
            if (state.Snippets.TryGetValue(name, out string? value))
            {
                return new TwHandlerResult(value);
            }
            else
            {
                return new TwHandlerResult(string.Empty);
            }
        }

        [TwStandardFunction("BR", "Inserts a line break into the page.")]
        public async Task<TwHandlerResult> BR(ITwEngineState state, int count = 1) => await NewLine(state, count);

        [TwStandardFunction("NL", "Inserts a line break into the page.")]
        public async Task<TwHandlerResult> NL(ITwEngineState state, int count = 1) => await NewLine(state, count);

        [TwStandardFunction("NewLine", "Inserts a line break into the page.")]
        public async Task<TwHandlerResult> NewLine(ITwEngineState state, int count = 1) //##NewLine([optional:default=1]count)
        {
            var lineBreaks = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                lineBreaks.Append("<br />");
            }
            return new TwHandlerResult(lineBreaks.ToString());
        }

        [TwStandardFunction("HR", "Inserts a horizontal rule into the page.")]
        public async Task<TwHandlerResult> HR(ITwEngineState state, int height = 1)
        {
            return new TwHandlerResult($"<hr class=\"my-{height}\">");
        }

        [TwStandardFunction("Navigation", "Displays the navigation text for the current page.")]
        public async Task<TwHandlerResult> Navigation(ITwEngineState state)
        {
            return new TwHandlerResult(state.Page.Navigation);
        }

        [TwStandardFunction("SystemEmojiList", "Displays a list of emojis for the specified category.")]
        public async Task<TwHandlerResult> SystemEmojiList(ITwEngineState state)
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
                var emojis = await EmojiRepository.GetEmojisByCategory(category);

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

            return new TwHandlerResult(html.ToString())
            {
                Instructions = [HandlerResultInstruction.DisallowNestedProcessing]
            };
        }

        [TwStandardFunction("SystemEmojiCategoryList", "Displays a list of emoji categories.")]
        public async Task<TwHandlerResult> SystemEmojiCategoryList(ITwEngineState state)
        {
            var categories = await EmojiRepository.GetEmojiCategoriesGrouped();

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

            return new TwHandlerResult(html.ToString())
            {
                Instructions = [HandlerResultInstruction.DisallowNestedProcessing]
            };
        }
    }
}
