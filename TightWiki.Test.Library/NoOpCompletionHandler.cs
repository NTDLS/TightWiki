using TightWiki.Plugin.Interfaces;

namespace TightWiki.Test.Library
{
    public class NoOpCompletionHandler
    //: ITwCompletionHandler
    {
        public async Task Handle(ITwEngineState state)
        {
        }
    }
}
