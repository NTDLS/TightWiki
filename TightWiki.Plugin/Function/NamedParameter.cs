namespace TightWiki.Plugin.Function
{
    /// <summary>
    /// Defines a named parameter for a function, consisting of a name and a value.
    /// This is used to represent parameters that are passed to a function by name,
    /// allowing for more flexible and readable function calls. The name of the
    /// parameter is used to match it to the corresponding parameter in the function
    /// definition, while the value is the actual data that will be passed to the
    /// function when it is invoked.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    public class NamedParameter(string name, object? value)
    {
        /// <summary>
        /// Name of the parameter.
        /// </summary>
        public string Name { get; set; } = name;

        /// <summary>
        /// Value of the parameter.
        /// </summary>
        public object? Value { get; set; } = value;
    }
}
