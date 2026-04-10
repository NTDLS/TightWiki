using Microsoft.Extensions.Logging;
using TightWiki.Plugin.Engine;

namespace TightWiki.Plugin.Interfaces.Module.Handlers
{
    /// <summary>
    /// Handles exceptions thrown by the wiki engine.
    /// </summary>
    public interface ITwExceptionDescriptor
    {
        /// <summary>
        /// Called when an exception is thrown by the wiki engine.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="level">The log level of the exception.</param>
        /// <param name="text">Text that accompanies the exception.</param>
        /// <param name="ex">Optional exception, in the case that this was an actual exception.</param>
        Task<TwPluginResult> Handle(ITwEngineState state, LogLevel level, string text, Exception? ex = null);
    }
}
