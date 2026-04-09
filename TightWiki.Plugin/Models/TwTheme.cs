namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents a visual theme for the wiki, defining the CSS classes and asset files
    /// used to control the appearance of the navigation bar, branding, dropdowns, and editor.
    /// </summary>
    public class TwTheme
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

        /// <summary>
        /// The parsed list of CSS or asset file paths associated with this theme.
        /// </summary>
        public List<string> Files { get; set; } = new();
    }
}
