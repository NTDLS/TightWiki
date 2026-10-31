using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;

namespace TightWiki.Plugin.Engine
{
    /// <summary>
    /// Search results that respect the viewer's permissions.
    /// </summary>
    public static class TwSearchResults
    {
        /// <summary>
        /// Returns the pages that match the search terms and that the session holds Read permission for, best matches first.
        /// When there is no session, nothing is returned because there is no one to check permissions for.
        /// </summary>
        public static async Task<List<TwPage>> GetReadablePages(ITwSessionState? session,
            ITwPageRepository pageRepository, List<string> searchTerms, bool? allowFuzzyMatching = null)
        {
            var readablePages = new List<TwPage>();

            if (session == null)
            {
                return readablePages;
            }

            foreach (var page in await pageRepository.PageSearch(searchTerms, allowFuzzyMatching))
            {
                if (await session.HoldsPermission(page.Navigation, TwPermission.Read))
                {
                    readablePages.Add(page);
                }
            }

            return readablePages;
        }
    }
}
