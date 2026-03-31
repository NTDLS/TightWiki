namespace TightWiki.Plugin.Attributes
{
    /// <summary>
    /// Attribute to mark a class as a plugin module in the TightWiki plugin system.
    /// The class can contain functions and handlers that will be registered and executed by the engine.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class TwPluginAttribute
    : Attribute
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
        public int Order { get; }

        /// <summary>
        /// Creates a new instance of the attribute with the specified name and description.
        /// </summary>
        /// <param name="name">The user-friendly display name of the plugin.</param>
        /// <param name="description">The user-friendly display description of the plugin.</param>
        /// <param name="order">The order in which the functions and handlers in the plugin should be registered and executed.</param>
        public TwPluginAttribute(string name, string description, int order)
        {
            Name = name;
            Description = description;
            Order = order;
        }
    }
}
