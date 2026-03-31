namespace TightWiki.Plugin.Attributes.Handlers
{
    /// <summary>
    /// Attribute to mark a method as a Emoji handler in the TightWiki plugin system.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TwEmojiPluginHandlerAttribute
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
        public int Precedence { get; }

        /// <summary>
        /// Creates a new instance of the attribute with the specified name and description.
        /// </summary>
        /// <param name="name">The user-friendly display name of the handler.</param>
        /// <param name="description">The user-friendly display description of the handler.</param>
        /// <param name="precedence">The order in which the handler should be executed.</param>
        public TwEmojiPluginHandlerAttribute(string name, string description, int precedence)
        {
            Name = name;
            Description = description;
            Precedence = precedence;
        }
    }
}
