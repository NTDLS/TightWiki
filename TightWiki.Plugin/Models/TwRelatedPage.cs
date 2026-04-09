namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents a wiki page that is related to another page through shared tags or references,
    /// including a match count indicating the strength of the relationship.
    /// </summary>
    public partial class TwRelatedPage
    {
        /// <summary>
        /// The unique identifier of the related page.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The full name of the related page, including namespace prefix if applicable.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The URL-safe navigation path used to locate the related page.
        /// </summary>
        public string Navigation { get; set; } = string.Empty;

        /// <summary>
        /// A short description of the related page content.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// The number of shared tags or references between this page and the source page,
        /// used to indicate the strength of the relationship.
        /// </summary>
        public int Matches { get; set; }

        /// <summary>
        /// The number of items per page used when paginating related page lists.
        /// </summary>
        public int PaginationPageSize { get; set; }

        /// <summary>
        /// The total number of pages available when paginating related page lists.
        /// </summary>
        public int PaginationPageCount { get; set; }

        /// <summary>
        /// The display title of the related page, derived from the name by stripping the namespace prefix if present.
        /// </summary>
        public string Title
        {
            get
            {
                if (Name.Contains("::"))
                {
                    return Name.Substring(Name.IndexOf("::") + 2).Trim();
                }

                return Name;
            }
        }
    }
}