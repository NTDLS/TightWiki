namespace TightWiki.Plugin.Attributes.Handlers
{
    /// <summary>
    /// Attribute to mark a method as a markup handler in the TightWiki plugin system.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TwMarkupPluginHandlerAttribute
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
        /// Indicates that the function can be used by the lite wiki engine.
        /// </summary>
        public bool IsLitePermissiable { get; } = false;

        /// <summary>
        /// Indicates whether this function is a first-chance function.
        /// These functions are evaluated before any other functions, allowing them to
        /// short-circuit the evaluation process or provide special handling for certain cases.
        /// </summary>
        public bool IsFirstChance { get; } = false;

        /// <summary>
        /// Creates a new instance of the attribute with the specified name and description.
        /// </summary>
        /// <param name="name">The user-friendly display name of the handler.</param>
        /// <param name="description">The user-friendly display description of the handler.</param>
        /// <param name="precedence">The order in which the handler should be executed.</param>
        /// <param name="isFirstChance">Indicates whether this handler is a first-chance function, meaning it should be executed before all other handlers.</param>
        /// <param name="isLitePermissiable">Indicates whether the handler is allowed in lite mode.</param>
        public TwMarkupPluginHandlerAttribute(string name, string description, int precedence = 1, bool isFirstChance = false, bool isLitePermissiable = false)
        {
            Name = name;
            Description = description;
            Precedence = precedence;
            IsLitePermissiable = isLitePermissiable;
            IsFirstChance = isFirstChance;
        }
    }
}
