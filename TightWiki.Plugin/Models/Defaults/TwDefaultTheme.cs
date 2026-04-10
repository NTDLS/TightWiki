namespace TightWiki.Plugin.Models.Defaults
{
    /// <summary>
    /// Represents a default theme definition used to seed the database with built-in themes on first run,
    /// defining the CSS classes and asset files that control the visual appearance of the wiki.
    /// </summary>
    public class TwDefaultTheme
    {
        /// <summary>
        /// The unique name of this theme.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// A delimited list of CSS or asset file paths associated with this theme.
        /// </summary>
        public string DelimitedFiles { get; set; } = string.Empty;

        /// <summary>
        /// The CSS class applied to the navigation bar element.
        /// </summary>
        public string ClassNavBar { get; set; } = string.Empty;

        /// <summary>
        /// The CSS class applied to navigation link elements.
        /// </summary>
        public string ClassNavLink { get; set; } = string.Empty;

        /// <summary>
        /// The CSS class applied to dropdown menu elements.
        /// </summary>
        public string ClassDropdown { get; set; } = string.Empty;

        /// <summary>
        /// The CSS class applied to the site branding element.
        /// </summary>
        public string ClassBranding { get; set; } = string.Empty;

        /// <summary>
        /// The name of the editor theme to use when rendering the wiki markup editor.
        /// </summary>
        public string EditorTheme { get; set; } = string.Empty;
    }
}