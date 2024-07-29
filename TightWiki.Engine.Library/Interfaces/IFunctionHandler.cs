using TightWiki.EngineFunction;

namespace TightWiki.Engine.Library.Interfaces
{
    public interface IFunctionHandler
    {
        /// <summary>
        /// Returns a collection of function prototypes.
        /// </summary>
        /// <returns></returns>
        public FunctionPrototypeCollection Prototypes { get; }

        /// <summary>
        /// When a function prototype is found, this function is called to process the call.
        /// </summary>
        public HandlerResult Handle(IWikifierSession wikifierSession, FunctionCall function, string scopeBody);
    }
}
