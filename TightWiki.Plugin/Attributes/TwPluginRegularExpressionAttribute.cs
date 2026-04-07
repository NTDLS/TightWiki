namespace TightWiki.Plugin.Attributes
{
    /// <summary>
    /// Attribute to mark a class as a plugin module in the TightWiki plugin system.
    /// The class can contain functions and handlers that will be registered and executed by the engine.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class TwPluginRegularExpressionAttribute
        : Attribute
    {
        /// <summary>
        /// The regular expression to be used by the plugin.
        /// </summary>
        public string Pattern { get; }

        /// <summary>
        /// Whether the regex will be matched in multi-line mode or not.
        /// </summary>
        public bool Multiline { get; set; }

        /// <summary>
        /// Creates a new instance of the attribute with the specified regular expression.
        /// </summary>
        /// <param name="expression">The regular expression to be used by the plugin.</param>
        /// <param name="multiline">Whether the regex will be matched in multi-line mode or not.</param>
        public TwPluginRegularExpressionAttribute(string expression, bool multiline = false)
        {
            Pattern = expression;
            Multiline = multiline;
        }
    }
}
