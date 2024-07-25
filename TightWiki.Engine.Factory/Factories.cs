using TightWiki.Engine;
using TightWiki.Engine.Library.Interfaces;
using TightWiki.Library.Interfaces;
using static TightWiki.Engine.Library.Constants;

namespace TightWiki
{
    public static class Factories
    {
        public static IWikifier CreateWikifier(ISessionState? sessionState, IPage page, int? pageRevision = null, WikiMatchType[]? omitMatches = null)
        {
            return new Wikifier(new Engine.Handlers.FunctionsHandler(), sessionState, page, pageRevision, omitMatches);
        }
    }
}
