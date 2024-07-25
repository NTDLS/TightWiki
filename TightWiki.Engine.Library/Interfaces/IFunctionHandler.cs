using TightWiki.EngineFunction;

namespace TightWiki.Engine.Library.Interfaces
{
    public interface IFunctionHandler
    {
        public HandlerResult Handle(IWikifier wikifier, FunctionCall function);
    }
}
