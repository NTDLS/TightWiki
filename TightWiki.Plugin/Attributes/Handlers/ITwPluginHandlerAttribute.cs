using TightWiki.Plugin.Interfaces;

namespace TightWiki.Plugin.Attributes.Handlers
{
    /// <summary>
    /// Base interface for attributes that describe handlers (e.g., markup handlers, heading handlers, etc.)
    /// in the TightWiki plugin system. This interface provides a common structure for handler attributes,
    /// ensuring that they all include a friendly name and an optional description.
    /// </summary>
    public interface ITwPluginHandlerAttribute
        : ITwPluginAttribute
    {

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
