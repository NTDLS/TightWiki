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
        public string Expression { get; }

        /// <summary>
        /// Creates a new instance of the attribute with the specified regular expression.
        /// </summary>
        /// <param name="expression">The regular expression to be used by the plugin.</param>
        public TwPluginRegularExpressionAttribute(string expression)
        {
            Expression = expression;
        }
    }
}
