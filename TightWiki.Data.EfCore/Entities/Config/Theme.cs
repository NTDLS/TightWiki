namespace TightWiki.Data.EfCore.Entities.Config
{
    /// <summary>
    /// A selectable UI theme (Config.Theme). The primary key is the theme's <see cref="Name"/> - there is no
    /// surrogate integer identifier in the real schema.
    /// </summary>
    public class Theme
    {
        /// <summary>
        /// The unique name of this theme. This is the primary key.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// A semicolon-delimited list of CSS/JS files that make up this theme.
        /// </summary>
        public string DelimitedFiles { get; set; } = string.Empty;

        /// <summary>
        /// The CSS class applied to the navigation bar.
        /// </summary>
        public string ClassNavBar { get; set; } = string.Empty;

        /// <summary>
        /// The CSS class applied to navigation links.
        /// </summary>
        public string ClassNavLink { get; set; } = string.Empty;

        /// <summary>
        /// The CSS class applied to dropdown menus.
        /// </summary>
        public string ClassDropdown { get; set; } = string.Empty;

        /// <summary>
        /// The CSS class applied to the branding/logo area.
        /// </summary>
        public string ClassBranding { get; set; } = string.Empty;

        /// <summary>
        /// The name of the code editor theme to use alongside this UI theme.
        /// </summary>
        public string EditorTheme { get; set; } = string.Empty;
    }
}
