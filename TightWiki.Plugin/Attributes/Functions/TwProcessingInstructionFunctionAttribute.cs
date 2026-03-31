namespace TightWiki.Plugin.Attributes.Functions
{
    /// <summary>
    /// Specifies that a method implements a processing instruction function for TightWiki syntax parsing.
    /// </summary>
    /// <remarks>Apply this attribute to methods that define custom processing instruction functions in
    /// TightWiki. Processing instruction functions can be used to extend or modify the behavior of the parser by
    /// handling special wiki syntax constructs.</remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TwProcessingInstructionFunctionAttribute
            : Attribute, ITwFunctionDescriptorAttribute
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
        public string Demarcation { get; } = "@@";

        /// <summary>
        /// Creates a new instance of the attribute with the specified name and description.
        /// </summary>
        public TwProcessingInstructionFunctionAttribute(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}
