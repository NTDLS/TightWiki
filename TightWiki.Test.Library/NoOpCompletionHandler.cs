using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Handlers;

namespace TightWiki.Test.Library
{
    public class NoOpCompletionHandler
        : ITwCompletionHandler
    {
        public async Task Handle(ITwEngineState state)
        {
        }
    }
}
