namespace TightWiki.Plugin.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class TwPluginModuleAttribute
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
        public TwPluginModuleAttribute(string name, string description, int order)
        {
            Name = name;
            Description = description;
            Order = order;
        }
    }
}
