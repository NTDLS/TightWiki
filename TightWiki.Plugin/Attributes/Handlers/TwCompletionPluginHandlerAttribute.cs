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
        /// Creates a new instance of the attribute with the specified name and description.
        /// </summary>
        public TwCompletionPluginHandlerAttribute(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}
