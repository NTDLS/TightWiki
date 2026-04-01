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
    }
}
