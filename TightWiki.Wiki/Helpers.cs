using TightWiki.Caching;
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
        /// <param name="context"></param>
        /// <param name="query"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        public static int UpsertPage(Page page, ISessionState? context = null)
        {
            bool isNewlyCreated = page.Id == 0;

            page.Id = PageRepository.SavePage(page);

            RefreshPageMetadata(page, context);

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
        /// <param name="context"></param>
        /// <param name="query"></param>
        /// <param name="page"></param>
        public static void RefreshPageMetadata(Page page, ISessionState? context = null)
        {
            var wikifier = Factories.CreateWikifier(context, page, null, [WikiMatchType.Function]);

            PageRepository.UpdatePageTags(page.Id, wikifier.Tags);
            PageRepository.UpdatePageProcessingInstructions(page.Id, wikifier.ProcessingInstructions);

            var pageTokens = wikifier.ParsePageTokens().Select(o =>
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
    }
}
