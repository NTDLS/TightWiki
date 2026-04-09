namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents aggregate statistics for a single wiki namespace,
    /// including the total number of pages it contains.
    /// </summary>
    public class TwNamespaceStat
    {
        /// <summary>
        /// The name of the namespace.
        /// </summary>
        public string Namespace { get; set; } = string.Empty;

        /// <summary>
        /// The total number of pages within this namespace.
        /// </summary>
        public int CountOfPages { get; set; }

        /// <summary>
        /// The total number of pages available when paginating namespace statistics lists.
        /// </summary>
        public int PaginationPageCount { get; set; }
    }
}