namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents a single item in a tag cloud, storing the tag name, its rendered HTML,
    /// and a rank value used to determine the visual weight of the tag in the cloud display.
    /// </summary>
    public class TwTagCloudItem
    {
        /// <summary>
        /// The name of the tag displayed in the cloud.
        /// </summary>
        public string Name = "";

        /// <summary>
        /// The pre-rendered HTML for this tag cloud item, including styling based on its rank.
        /// </summary>
        public string HTML = "";

        /// <summary>
        /// The rank of this tag, representing its relative frequency or importance.
        /// Higher ranks result in more prominent visual styling in the tag cloud.
        /// </summary>
        public int Rank = 0;

        /// <summary>
        /// Initializes a new tag cloud item with the specified name, rank, and rendered HTML.
        /// </summary>
        /// <param name="name">The name of the tag.</param>
        /// <param name="rank">The rank indicating the tag's relative frequency or importance.</param>
        /// <param name="html">The pre-rendered HTML for this tag cloud item.</param>
        public TwTagCloudItem(string name, int rank, string html)
        {
            Name = name;
            HTML = html;
            Rank = rank;
        }

        /// <summary>
        /// Compares two tag cloud items alphabetically by name, for use in sorting operations.
        /// </summary>
        /// <param name="x">The first tag cloud item to compare.</param>
        /// <param name="y">The second tag cloud item to compare.</param>
        /// <returns>A negative value if x precedes y, zero if equal, or a positive value if x follows y.</returns>
        public static int CompareItem(TwTagCloudItem x, TwTagCloudItem y)
        {
            return string.Compare(x.Name, y.Name);
        }
    }
}