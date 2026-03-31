namespace TightWiki.Plugin.Attributes.Handlers
{
    /// <summary>
    /// Specifies that a method is a completion handler for the wikification workflow.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TwCompletionPluginHandlerAttribute
            : Attribute, ITwPluginHandlerAttribute
    {
        /// <summary>
        /// The user-friendly display name of the hander.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The user-friendly display description of the hander.
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
        /// <param name="name">The user-friendly display name of the handler.</param>
        /// <param name="description">The user-friendly display description of the handler.</param>
        /// <param name="order">The order in which the handler should be executed.</param>
        public TwCompletionPluginHandlerAttribute(string name, string description, int order)
        {
            Name = name;
            Description = description;
            Order = order;
        }
    }
}
