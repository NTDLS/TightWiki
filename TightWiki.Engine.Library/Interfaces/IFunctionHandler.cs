using TightWiki.Engine.Function;

namespace TightWiki.Engine.Library.Interfaces
{
    /// <summary>
    /// Base function handler for standard, post-processing, scoped and processing-instruction functions.
    /// </summary>
    public interface IFunctionHandler
    {
        /// <summary>
        /// Returns a collection of function prototypes.
        /// </summary>
        /// <returns></returns>
        public FunctionPrototypeCollection Prototypes { get; }

        /// <summary>
        /// Called to handle function calls when proper prototypes are matched.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="function">The parsed function call and all its parameters and their values.</param>
        /// <param name="scopeBody">For scope functions, this is the text that the function is designed to affect.</param>
        public HandlerResult Handle(ITightEngineState state, FunctionCall function, string? scopeBody = null);
    }
}
