using Microsoft.Extensions.Logging;
using TightWiki.Plugin.Engine;

namespace TightWiki.Plugin.Interfaces.Module.Handlers
{
    /// <summary>
    /// Handles exceptions thrown by the wiki engine.
    /// </summary>
    public interface ITwExceptionPlugin
        : ITwPlugin
    {
        /// <summary>
        /// Called when an exception is thrown by the wiki engine.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="customText">Text that accompanies the exception.</param>
        /// <param name="ex">Optional exception, in the case that this was an actual exception.</param>
        Task<TwPluginResult> Handle(ITwEngineState state, LogLevel level, string text, Exception? ex = null);
    }
}
