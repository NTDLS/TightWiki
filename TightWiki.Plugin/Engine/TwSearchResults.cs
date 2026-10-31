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
        /// When there is no session (no one is known to be viewing), the permissions of an anonymous visitor are used.
        /// </summary>
        public static async Task<List<TwPage>> GetReadablePages(ITwSessionState? session,
            ITwPageRepository pageRepository, ITwUsersRepository usersRepository, List<string> searchTerms, bool? allowFuzzyMatching = null)
        {
            var readablePages = new List<TwPage>();

            foreach (var page in await pageRepository.PageSearch(searchTerms, allowFuzzyMatching))
            {
                bool canRead = session != null
                    ? await session.HoldsPermission(page.Navigation, TwPermission.Read)
                    : await usersRepository.DoesAnonymousHoldPermission(TwPermission.Read, page);

                if (canRead)
                {
                    readablePages.Add(page);
                }
            }

            return readablePages;
        }
    }
}
