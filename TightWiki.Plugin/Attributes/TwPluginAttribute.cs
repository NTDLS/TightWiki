using TightWiki.Plugin.Interfaces;

namespace TightWiki.Plugin.Attributes
{
    /// <summary>
    /// Attribute to mark a class as a plugin module in the TightWiki plugin system.
    /// The class can contain functions and handlers that will be registered and executed by the engine.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class TwPluginAttribute
        : Attribute, ITwPluginAttribute
    {
        /// <summary>
        /// The user-friendly display name of the plugin module.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The user-friendly display description of the plugin module.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// The order in which the functions and handlers in the plugin module should be registered and executed.
        /// Lower values indicate higher priority.
        /// </summary>
        public int Precedence { get; }

        /// <summary>
        /// Indicates that the function can be used by the lite wiki engine.
        /// </summary>
        public bool IsLitePermissiable { get; } = false;

        /// <summary>
        /// Creates a new instance of the attribute with the specified name and description.
        /// </summary>
        /// <param name="name">The user-friendly display name of the plugin.</param>
        /// <param name="description">The user-friendly display description of the plugin.</param>
        /// <param name="precedence">The order in which the functions and handlers in the plugin should be registered and executed.</param>
        /// <param name="isLitePermissiable">Indicates that the function can be used by the lite wiki engine.</param>
        public TwPluginAttribute(string name, string description, int precedence = 1, bool isLitePermissiable = false)
        {
            Name = name;
            Description = description;
            Precedence = precedence;
            IsLitePermissiable = isLitePermissiable;
        }
    }
}
