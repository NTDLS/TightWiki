using TightWiki.Engine.Library.Interfaces;

namespace TightWiki.Test.Library
{
    public class NoOpCompletionHandler
        : ICompletionHandler
    {
        public async Task Complete(ITightEngineState state)
        {
        }
    }
}
