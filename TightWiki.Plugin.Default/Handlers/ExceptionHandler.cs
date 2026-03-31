using Microsoft.Extensions.Logging;
using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Attributes.Handlers;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Module.Handlers;

namespace TightWiki.Plugin.Default.Handlers
{
    /// <summary>
    /// Handles exceptions thrown by the wiki engine.
    /// </summary>
    [TwPlugin("Default exception handler", "Handles exceptions thrown by the wiki engine.", 1000)]
    public class ExceptionHandler
        : ITwExceptionPlugin
    {
        /// <summary>
        /// Called when an exception is thrown by the wiki engine.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="ex">Optional exception, in the case that this was an actual exception.</param>
        /// <param name="customText">Text that accompanies the exception.</param>
        [TwExceptionPluginHandler("Default exception handler", "Handles exceptions thrown by the wiki engine.")]
        public async Task<TwPluginResult> Handle(ITwEngineState state, LogLevel level, string text, Exception? ex = null)
        {
            if (ex != null)
            {
                state.Logger.Log(level, text, ex);
            }
            else
            {
                state.Logger.Log(level, text);
            }

            return new TwPluginResult();
        }
    }
}
