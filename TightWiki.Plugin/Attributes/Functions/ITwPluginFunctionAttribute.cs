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
    public interface ITwPluginFunctionAttribute
    {
        /// The user-friendly display name of the hander.
        string Name { get; }

        /// The user-friendly display description of the hander.
        string Description { get; }

        /// <summary>
        /// The order in which the functions and handlers in the plugin module should be registered and executed.
        /// Lower values indicate higher priority.
        /// </summary>
        public int Precedence { get; }

        /// <summary>
        /// Indicates that the function is a post-process function, which is evaluated after all other functions have been processed.
        /// </summary>
        bool IsPostProcess { get; }

        /// <summary>
        /// The prefix used to demarcate the function in the wiki syntax, such as "##", "@@", etc for TightWiki functions.
        /// </summary>
        string Demarcation { get; }

        /// <summary>
        /// Indicates whether this function is a first-chance function.
        /// These functions are evaluated before any other functions, allowing them to
        /// short-circuit the evaluation process or provide special handling for certain cases.
        /// </summary>
        bool IsFirstChance { get; }

        /// Indicates that the function can be used by the lite wiki engine.
        bool IsLitePermissiable { get; }
    }
}
