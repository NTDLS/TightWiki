using TightWiki.Plugin.Interfaces;

namespace TightWiki.Test.Library
{
    public class NoOpCompletionHandler
        : ICompletionHandler
    {
        public async Task Complete(ITwEngineState state)
        {
        }
    }
}
