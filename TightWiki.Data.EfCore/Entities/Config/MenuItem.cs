namespace TightWiki.Data.EfCore.Entities.Config
{
    /// <summary>
    /// A single item in the wiki's navigation menu (Config.MenuItem).
    /// </summary>
    public class MenuItem
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
