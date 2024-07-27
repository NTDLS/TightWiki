using DuoVia.FuzzyStrings;
using NTDLS.Helpers;
using TightWiki.Caching;
using TightWiki.Engine.Library;
using TightWiki.Engine.Library.Interfaces;
using TightWiki.Library.Interfaces;
using TightWiki.Models.DataModels;
using TightWiki.Repository;
using static TightWiki.Engine.Library.Constants;

namespace TightWiki.Wiki
{
    public class Helpers
    {
        /// <summary>
        /// Inserts a new page if Page.Id == 0, other wise updates the page. All metadata is written to the database.
        /// </summary>
        /// <param name="sessionState"></param>
        /// <param name="query"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        public static int UpsertPage(Page page, ISessionState? sessionState = null)
        {
            bool isNewlyCreated = page.Id == 0;

            page.Id = PageRepository.SavePage(page);

            RefreshPageMetadata(page, sessionState);

            if (isNewlyCreated)
            {
                //This will update the PageId of references that have been saved to the navigation link.
                PageRepository.UpdateSinglePageReference(page.Navigation, page.Id);
            }

            return page.Id;
        }

        /// <summary>
        /// Rebuilds the page and writes all aspects to the database.
        /// </summary>
        /// <param name="sessionState"></param>
        /// <param name="query"></param>
        /// <param name="page"></param>
        public static void RefreshPageMetadata(Page page, ISessionState? sessionState = null)
        {
            var wikifier = Factories.CreateWikifier(sessionState, page, null, [WikiMatchType.Function]);

            PageRepository.UpdatePageTags(page.Id, wikifier.Tags);
            PageRepository.UpdatePageProcessingInstructions(page.Id, wikifier.ProcessingInstructions);

            var pageTokens = ParsePageTokens(wikifier).Select(o =>
                      new PageToken
                      {
                          PageId = page.Id,
                          Token = o.Token,
                          DoubleMetaphone = o.DoubleMetaphone,
                          Weight = o.Weight
                      }).ToList();

            PageRepository.SavePageSearchTokens(pageTokens);

            PageRepository.UpdatePageReferences(page.Id, wikifier.OutgoingLinks);

            WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.Page, [page.Id]));
            WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.Page, [page.Navigation]));
        }

        public static List<WeightedToken> ParsePageTokens(IWikifier wikifier)
        {
            var allTokens = new List<WeightedToken>();

            allTokens.AddRange(ComputeParsedPageTokens(wikifier.ProcessedBody, 1));
            allTokens.AddRange(ComputeParsedPageTokens(wikifier.Page.Description, 1.2));
            allTokens.AddRange(ComputeParsedPageTokens(string.Join(" ", wikifier.Tags), 1.4));
            allTokens.AddRange(ComputeParsedPageTokens(wikifier.Page.Name, 1.6));

            allTokens = allTokens.GroupBy(o => o.Token).Select(o => new WeightedToken
            {
                Token = o.Key,
                DoubleMetaphone = o.Key.ToDoubleMetaphone(),
                Weight = o.Sum(g => g.Weight)
            }).ToList();

            return allTokens;
        }

        internal static List<WeightedToken> ComputeParsedPageTokens(string content, double weightMultiplier)
        {
            var searchConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Search");

            var exclusionWords = searchConfig?.Value<string>("Word Exclusions")?.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Distinct() ?? new List<string>();
            var strippedContent = Html.StripHtml(content);
            var tokens = strippedContent.Split([' ', '\n', '\t', '-', '_']).ToList<string>().ToList();

            if (searchConfig?.Value<bool>("Split Camel Case") == true)
            {
                var casedTokens = new List<string>();

                foreach (var token in tokens)
                {
                    var splitTokens = Text.SeperateCamelCase(token).Split(' ');
                    if (splitTokens.Count() > 1)
                    {
                        foreach (var lowerToken in splitTokens)
                        {
                            casedTokens.Add(lowerToken.ToLower());
                        }
                    }
                }

                tokens.AddRange(casedTokens);
            }

            tokens = tokens.ConvertAll(d => d.ToLower());

            tokens.RemoveAll(o => exclusionWords.Contains(o));

            var searchTokens = (from w in tokens
                                group w by w into g
                                select new WeightedToken
                                {
                                    Token = g.Key,
                                    Weight = g.Count() * weightMultiplier
                                }).ToList();

            return searchTokens.Where(o => string.IsNullOrWhiteSpace(o.Token) == false).ToList();
        }
    }
}
