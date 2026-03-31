using TightWiki.Plugin.Attributes;

namespace TightWiki.Plugin.Interfaces.Module
{
    /// <summary>
    /// Base interface for a class that is a plugin module in the TightWiki plugin system.
    /// The class can contain functions and handlers that will be registered and executed by the engine.
    /// </summary>
    public interface ITwEngineModule
    {
        /// <summary>
        /// The type of the class that implements the plugin module.
        /// </summary>
        Type DeclaringType { get; }

        /// <summary>
        /// The attribute of the plugin module class, containing information such as the module name, description, and order.
        /// </summary>
        TwPluginAttribute Attribute { get; }

        /// <summary>
        /// An instance of the plugin module class, used to invoke any functions or handlers that it contains.
        /// </summary>
        ITwPlugin Instance { get; }
    }
}
