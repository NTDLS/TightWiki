using DuoVia.FuzzyStrings;
using NTDLS.Helpers;
using NTDLS.SqliteDapperWrapper;
using TightWiki.Caching;
using TightWiki.Models.DataModels;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki.Repository
{
    public static class RepositoryHelpers
    {
        /// <summary>
        /// Inserts a new page if Page.Id == 0, other wise updates the page. All metadata is written to the database.
        /// </summary>
        public static async Task<int> UpsertPage(ITwEngine wikifier, ITwSharedLocalizationText localizer, WikiPage page, ITwSessionState? sessionState = null)
        {
            bool isNewlyCreated = page.Id == 0;

            page.Id = await PageRepository.SavePage(page);

            await RefreshPageMetadata(wikifier, localizer, page, sessionState);

            if (isNewlyCreated)
            {
                //This will update the PageId of references that have been saved to the navigation link.
                await PageRepository.UpdateSinglePageReference(page.Navigation, page.Id);
            }

            return page.Id;
        }

        /// <summary>
        /// Rebuilds the page and writes all aspects to the database.
        /// </summary>
        /// <param name="sessionState"></param>
        /// <param name="query"></param>
        /// <param name="page"></param>
        public static async Task RefreshPageMetadata(ITwEngine wikifier, ITwSharedLocalizationText localizer, WikiPage page, ITwSessionState? sessionState = null)
        {
            //We omit function calls from the tokenization process because they are too dynamic for static searching.
            var state = await wikifier.Transform(localizer, sessionState, page, null, [WikiMatchType.StandardFunction]);

            await PageRepository.UpdatePageTags(page.Id, state.Tags);
            await PageRepository.UpdatePageProcessingInstructions(page.Id, state.ProcessingInstructions);

            var pageTokens = (await ParsePageTokens(state)).Select(o =>
                      new PageToken
                      {
                          PageId = page.Id,
                          Token = o.Token,
                          DoubleMetaphone = o.DoubleMetaphone,
                          Weight = o.Weight
                      }).ToList();

            await PageRepository.SavePageSearchTokens(pageTokens);

            await PageRepository.UpdatePageReferences(page.Id, state.OutgoingLinks);

            WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.Page, [page.Id]));
            WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.Page, [page.Navigation]));
        }

        public static async Task<List<AggregatedSearchToken>> ParsePageTokens(ITwEngineState state)
        {
            var parsedTokens = new List<WeightedSearchToken>();

            parsedTokens.AddRange(await ComputeParsedPageTokens(state.HtmlResult, 1));
            parsedTokens.AddRange(await ComputeParsedPageTokens(state.Page.Description, 1.2));
            parsedTokens.AddRange(await ComputeParsedPageTokens(string.Join(" ", state.Tags), 1.4));
            parsedTokens.AddRange(await ComputeParsedPageTokens(state.Page.Name, 1.6));

            var aggregatedTokens = parsedTokens.GroupBy(o => o.Token).Select(o => new AggregatedSearchToken
            {
                Token = o.Key,
                DoubleMetaphone = o.Key.ToDoubleMetaphone(),
                Weight = o.Sum(g => g.Weight)
            }).ToList();

            return aggregatedTokens;
        }

        internal static async Task<List<WeightedSearchToken>> ComputeParsedPageTokens(string content, double weightMultiplier)
        {
            var searchConfig = await ConfigurationRepository.GetConfigurationEntryValuesByGroupName(WikiConfigurationGroup.Membership);

            var exclusionWords = searchConfig?.Value<string>("Word Exclusions")?
                .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries).Distinct() ?? new List<string>();
            var strippedContent = Html.StripHtml(content);

            var tokens = strippedContent.Split([' ', '\n', '\t', '-', '_']).ToList();

            if (searchConfig?.Value<bool>("Split Camel Case") == true)
            {
                var allSplitTokens = new List<string>();

                foreach (var token in tokens)
                {
                    var splitTokens = Text.SplitCamelCase(token);
                    if (splitTokens.Count > 1)
                    {
                        splitTokens.ForEach(t => allSplitTokens.Add(t));
                    }
                }

                tokens.AddRange(allSplitTokens);
            }

            tokens = tokens.ConvertAll(d => d.ToLowerInvariant());

            tokens.RemoveAll(o => exclusionWords.Contains(o));

            var searchTokens = (from w in tokens
                                group w by w into g
                                select new WeightedSearchToken
                                {
                                    Token = g.Key,
                                    Weight = g.Count() * weightMultiplier
                                }).ToList();

            return searchTokens.Where(o => string.IsNullOrWhiteSpace(o.Token) == false).ToList();
        }

        /// <summary>
        /// Fills in a custom orderby on a given sql script.
        /// </summary>
        public static string TransposeOrderby(string filename, string? orderBy, string? orderByDirection)
        {
            var script = EmbeddedResource.Load(filename);

            if (string.IsNullOrEmpty(orderBy))
            {
                return script;
            }

            string beginParentTag = "--CUSTOM_ORDER_BEGIN::";
            string endParentTag = "--::CUSTOM_ORDER_BEGIN";

            string beginConfigTag = "--CONFIG::";
            string endConfigTag = "--::CONFIG";

            while (true)
            {
                int beginParentIndex = script.IndexOf(beginParentTag, StringComparison.InvariantCultureIgnoreCase);
                int endParentIndex = script.IndexOf(endParentTag, StringComparison.InvariantCultureIgnoreCase);

                if (beginParentIndex > 0 && endParentIndex > beginParentIndex)
                {
                    var sectionText = script.Substring(beginParentIndex + beginParentTag.Length, (endParentIndex - beginParentIndex) - endParentTag.Length).Trim();

                    int beginConfigIndex = sectionText.IndexOf(beginConfigTag, StringComparison.InvariantCultureIgnoreCase);
                    int endConfigIndex = sectionText.IndexOf(endConfigTag, StringComparison.InvariantCultureIgnoreCase);

                    if (beginConfigIndex >= 0 && endConfigIndex > beginConfigIndex)
                    {
                        var configText = sectionText.Substring(beginConfigIndex + beginConfigTag.Length, (endConfigIndex - beginConfigIndex) - endConfigTag.Length).Trim();

                        var configs = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

                        configs.Remove("/^");
                        configs.Remove("*/");

                        foreach (var line in configText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (line == "/*" || line == "*/" || line.StartsWith("--"))
                            {
                                continue;
                            }

                            int idx = line.IndexOf('=');
                            if (idx > -1)
                            {
                                var key = line.Substring(0, idx).Trim();
                                var value = line.Substring(idx + 1).Trim();
                                configs[key] = value;
                            }
                            else
                            {
                                throw new Exception($"Invalid configuration line in '{filename}': {line}");
                            }
                        }

                        if (!configs.TryGetValue(orderBy, out string? field))
                        {
                            throw new Exception($"No order by mapping was found in '{filename}' for the field '{orderBy}'.");
                        }

                        script = script.Substring(0, beginParentIndex)
                            + $"ORDER BY\r\n\t{field} "
                            + (string.Equals(orderByDirection, "asc", StringComparison.InvariantCultureIgnoreCase) ? "asc" : "desc")
                            + script.Substring(endParentIndex + endParentTag.Length);
                    }
                    else
                    {
                        throw new Exception($"No order configuration was found in '{filename}'.");
                    }
                }
                else
                {
                    break;
                }
            }

            return script;
        }
    }
}
