namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents a single item in the wiki's navigation menu,
    /// defining its display name, link target, and display order.
    /// </summary>
    public partial class TwMenuItem
    {
        /// <summary>
        /// The unique identifier for this menu item.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The display name of this menu item as shown in the navigation bar.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The URL or navigation path this menu item links to.
        /// </summary>
        public string Link { get; set; } = string.Empty;

        /// <summary>
        /// The position of this menu item in the navigation bar, where lower values appear first.
        /// </summary>
        public int Ordinal { get; set; }
    }
}