using TightWiki.Plugin.Interfaces;

namespace TightWiki.Plugin.Attributes.Functions
{
    /// <summary>
    /// Base interface for function descriptor attributes.
    /// This interface defines the common properties that all function descriptor attributes must implement,
    /// such as Name, Description, IsFirstChance, and Demarcation.
    /// These properties provide metadata about the function, which can be used for documentation, user interfaces,
    /// or other purposes within the TightWiki plugin system.
    /// </summary>
    public interface ITwFunctionPluginAttribute
        : ITwPluginAttribute
    {
        /// <summary>
        /// Indicates whether this function is a first-chance function.
        /// These functions are evaluated before any other functions, allowing them to
        /// short-circuit the evaluation process or provide special handling for certain cases.
        /// </summary>
        bool IsFirstChance { get; }

        /// <summary>
        /// The prefix used to demarcate the function in the wiki syntax, such as "##", "@@", etc for TightWiki functions.
        /// </summary>
        string Demarcation { get; }
    }
}
