using TightWiki.Plugin.Interfaces;

namespace TightWiki.Plugin.Attributes.Handlers
{
    /// <summary>
    /// Base interface for attributes that describe handlers (e.g., markup handlers, heading handlers, etc.)
    /// in the TightWiki plugin system. This interface provides a common structure for handler attributes,
    /// ensuring that they all include a friendly name and an optional description.
    /// </summary>
    public interface ITwPluginHandlerAttribute
    {
        /// The user-friendly display name of the hander.
        string Name { get; }

        /// The user-friendly display description of the hander.
        string Description { get; }

        /// <summary>
        /// The order in which the functions and handlers in the plugin module should be registered and executed.
        /// Lower values indicate higher priority.
        /// </summary>
        public int Precedence { get; }

        /// <summary>
        /// Indicates whether this function is a first-chance function.
        /// These functions are evaluated before any other functions, allowing them to
        /// short-circuit the evaluation process or provide special handling for certain cases.
        /// </summary>
        bool IsFirstChance { get; }

        /// Indicates that the function can be used by the lite wiki engine.
        bool IsLitePermissiable { get; }
    }
}
