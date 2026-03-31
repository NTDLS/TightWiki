namespace TightWiki.Plugin.Attributes.Functions
{
    /// <summary>
    /// Specifies that a method represents a standard function in TightWiki, providing metadata such as a user-friendly
    /// name, description, and evaluation behavior.
    /// </summary>
    /// <remarks>Apply this attribute to methods to designate them as standard functions within the TightWiki
    /// system. The attribute enables the system to identify, describe, and control the evaluation order of functions,
    /// particularly for those that require special handling or early evaluation.</remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TwStandardFunctionPluginAttribute
            : Attribute, ITwFunctionPluginAttribute
    {
        /// <summary>
        /// The user-friendly display name of the function.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The user-friendly display description of the function.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Indicates whether this function is a first-chance function.
        /// These functions are evaluated before any other functions, allowing them to
        /// short-circuit the evaluation process or provide special handling for certain cases.
        /// </summary>
        public bool IsFirstChance { get; } = false;

        /// <summary>
        /// The prefix used to demarcate the function in the wiki syntax, such as "##", "@@", etc for TightWiki functions.
        /// </summary>
        public string Demarcation { get; } = "##";

        /// <summary>
        /// The order in which the functions and handlers in the plugin module should be registered and executed.
        /// Lower values indicate higher priority.
        /// </summary>
        public int Precedence { get; }

        /// <summary>
        /// Creates a new instance of the attribute with the specified name and description.
        /// </summary>
        /// <param name="name">The user-friendly display name of the function.</param>
        /// <param name="description">The user-friendly display description of the function.</param>
        /// <param name="precedence">The order in which the function should be executed.</param>
        /// <param name="isFirstChance">Indicates whether this function is a first-chance function, meaning it should be executed before all other functions.</param>
        public TwStandardFunctionPluginAttribute(string name, string description, int precedence, bool isFirstChance = false)
        {
            Name = name;
            Description = description;
            Precedence = precedence;
            IsFirstChance = isFirstChance;
        }
    }
}
