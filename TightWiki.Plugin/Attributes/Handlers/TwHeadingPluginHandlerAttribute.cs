namespace TightWiki.Plugin.Attributes.Handlers
{
    /// <summary>
    /// Attribute to mark a method as a heading handler in the TightWiki plugin system.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TwHeadingPluginHandlerAttribute
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
        public TwHeadingPluginHandlerAttribute(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}
