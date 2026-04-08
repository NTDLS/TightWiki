namespace TightWiki.Plugin.Interfaces
{
    /// <summary>
    /// Base interface for attributes that describe handlers (e.g., markup handlers, heading handlers, etc.)
    /// in the TightWiki plugin system. This interface provides a common structure for handler attributes,
    /// ensuring that they all include a friendly name and an optional description.
    /// </summary>
    public interface ITwPluginAttribute
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

        /// Indicates that the function can be used by the lite wiki engine.
        bool IsLitePermissiable { get; }
    }
}
