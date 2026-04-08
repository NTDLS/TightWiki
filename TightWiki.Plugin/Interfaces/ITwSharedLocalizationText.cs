using Microsoft.Extensions.Localization;

namespace TightWiki.Plugin.Interfaces
{
    /// <summary>
    /// Defines an interface for accessing localized text resources by key, with support for formatting and parameter
    /// substitution.
    /// </summary>
    /// <remarks>Implementations of this interface provide mechanisms to retrieve localized strings for a
    /// given key, optionally formatting them with supplied arguments. This interface is typically used to support
    /// internationalization and localization in applications, allowing text to be dynamically retrieved and formatted
    /// based on the current culture or language settings.</remarks>
    public interface ITwSharedLocalizationText
    {
        /// <summary>
        /// Gets the localized string associated with the specified key.
        /// </summary>
        /// <remarks>This indexer is typically used to retrieve localized resources for display in user
        /// interfaces. The returned <see cref="LocalizedString"/> may indicate whether the resource was found or
        /// provide a fallback value, depending on the implementation.</remarks>
        /// <param name="key">The key that identifies the localized string to retrieve. Cannot be null.</param>
        /// <returns>A <see cref="LocalizedString"/> representing the localized value for the specified key. If the key is not
        /// found, returns a <see cref="LocalizedString"/> indicating the missing resource.</returns>
        LocalizedString this[string key] { get; }

        /// <summary>
        /// Gets the localized string associated with the specified key, formatted with the provided arguments if
        /// applicable.
        /// </summary>
        /// <remarks>This indexer is typically used to retrieve and format localized resources for
        /// display. If the resource string contains format placeholders, the arguments are used to replace them using
        /// standard .NET formatting.</remarks>
        /// <param name="key">The key that identifies the resource to retrieve. Cannot be null.</param>
        /// <param name="arguments">An optional array of objects to format the localized string. Arguments are inserted into the string in place
        /// of format items, if present.</param>
        /// <returns>A LocalizedString containing the localized value for the specified key, formatted with the provided
        /// arguments if any. If the key is not found, returns a LocalizedString indicating the missing resource.</returns>
        LocalizedString this[string key, params object[] arguments] { get; }

        /// <summary>
        /// Retrieves the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key whose associated value is to be returned. Cannot be null or empty.</param>
        /// <returns>The value associated with the specified key, or null if the key does not exist.</returns>
        string Get(string key);

        /// <summary>
        /// Formats a localized string by substituting the specified arguments into the string resource identified by
        /// the given key.
        /// </summary>
        /// <remarks>The method retrieves a format string using the provided key and applies standard .NET
        /// formatting with the supplied arguments. The behavior when the key is not found may depend on the
        /// implementation.</remarks>
        /// <param name="key">The resource key that identifies the format string to be localized and formatted. Cannot be null or empty.</param>
        /// <param name="arguments">An array of objects to substitute into the format string. Each object replaces a corresponding format item
        /// in the string. Can be empty if the format string does not require arguments.</param>
        /// <returns>A formatted, localized string with the specified arguments substituted into the resource string. If the key
        /// is not found, the key itself may be returned.</returns>
        string Format(string key, params object?[] arguments);
    }
}
