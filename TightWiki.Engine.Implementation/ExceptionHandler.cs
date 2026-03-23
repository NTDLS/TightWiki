using Microsoft.Extensions.Logging;
using TightWiki.Engine.Library.Interfaces;

namespace TightWiki.Engine.Implementation
{
    /// <summary>
    /// Handles exceptions thrown by the wiki engine.
    /// </summary>
    public class ExceptionHandler
        : IExceptionHandler
    {
        /// <summary>
        /// Called when an exception is thrown by the wiki engine.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="ex">Optional exception, in the case that this was an actual exception.</param>
        /// <param name="customText">Text that accompanies the exception.</param>
        public void Log(ITightEngineState state, Exception? ex, string customText)
        {
            if (ex != null)
            {
                state.Logger.LogError(ex, customText);
            }

            state.Logger.LogError(customText);
        }
    }
}
