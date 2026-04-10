using TightWiki.Plugin.Interfaces;

namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents a wiki page, including its metadata, revision history, content, and search-related properties.
    /// </summary>
    public class TwPage
        : ITwPage
    {
        /// <summary>
        /// The unique identifier for this page. A value of 0 indicates the page has not been saved.
        /// </summary>
        public int Id { get; set; } = 0;

        /// <summary>
        /// The revision of this page that is being viewed. May not be the latest revision.
        /// </summary>
        public int Revision { get; set; }

        /// <summary>
        /// The most current revision of this page.
        /// </summary>
        public int MostCurrentRevision { get; set; }

        /// <summary>
        /// Returns true if the currently viewed revision is not the latest revision.
        /// </summary>
        public bool IsHistoricalVersion => Revision != MostCurrentRevision;

        /// <summary>
        /// Returns true if this page exists and has been loaded from the database.
        /// </summary>
        public bool Exists => Id > 0;

        /// <summary>
        /// The number of revisions newer than the currently viewed revision.
        /// </summary>
        public int HigherRevisionCount { get; set; }

        /// <summary>
        /// The number of revisions that have been soft-deleted for this page.
        /// </summary>
        public int DeletedRevisionCount { get; set; }

        /// <summary>
        /// The full name of the page, including namespace prefix if applicable.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// A brief summary of the changes made in this revision.
        /// </summary>
        public string ChangeSummary { get; set; } = string.Empty;

        /// <summary>
        /// The URL-safe navigation path used to locate this page.
        /// </summary>
        public string Navigation { get; set; } = string.Empty;

        /// <summary>
        /// A short description of the page content, used in search results and meta tags.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// A hash of the page content used to detect changes between revisions.
        /// </summary>
        public int DataHash { get; set; }

        /// <summary>
        /// Returns a truncated version of the description, cut at a word boundary near 64 characters and capped at 100 characters.
        /// </summary>
        public string EllipseDescription
        {
            get
            {
                int idealLength = 64;
                int maxLength = 100;

                if (Description.Length > idealLength)
                {
                    int spacePos = Description.IndexOf(' ', idealLength);
                    int tabPos = Description.IndexOf('\t', idealLength);

                    idealLength = spacePos > tabPos && tabPos > 0 ? tabPos : spacePos;
                    if (idealLength > 0 && idealLength < maxLength)
                    {
                        return Description.Substring(0, idealLength) + "...";
                    }
                }
                if (Description.Length > maxLength)
                {
                    return Description.Substring(0, maxLength) + "...";
                }
                return Description;
            }
        }

        /// <summary>
        /// The user ID of the user who originally created this page.
        /// </summary>
        public Guid CreatedByUserId { get; set; }

        /// <summary>
        /// The date and time this page was originally created.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// The user ID of the user who last modified this page.
        /// </summary>
        public Guid ModifiedByUserId { get; set; }

        /// <summary>
        /// The date and time this page was last modified.
        /// </summary>
        public DateTime ModifiedDate { get; set; }

        /// <summary>
        /// The date and time this page was deleted, if applicable.
        /// </summary>
        public DateTime DeletedDate { get; set; }

        /// <summary>
        /// The user ID of the user who deleted this page, if applicable.
        /// </summary>
        public Guid DeletedByUserId { get; set; }

        /// <summary>
        /// The account name of the user who deleted this page, if applicable.
        /// </summary>
        public string DeletedByUserName { get; set; } = string.Empty;

        /// <summary>
        /// The cumulative weight of search tokens associated with this page, used for ranking.
        /// </summary>
        public int TokenWeight { get; set; }

        /// <summary>
        /// The raw wiki markup body of this page revision.
        /// </summary>
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// The account name of the user who originally created this page.
        /// </summary>
        public string CreatedByUserName { get; set; } = string.Empty;

        /// <summary>
        /// The account name of the user who last modified this page.
        /// </summary>
        public string ModifiedByUserName { get; set; } = string.Empty;

        /// <summary>
        /// The total number of times this page has been viewed.
        /// </summary>
        public int TotalViewCount { get; set; }

        /// <summary>
        /// The total number of pages available when paginating results that include this page.
        /// </summary>
        public int PaginationPageCount { get; set; }

        /// <summary>
        /// The relevance match score for this page in a search result set.
        /// </summary>
        public decimal Match { get; set; }

        /// <summary>
        /// The weight assigned to this page for ranking purposes.
        /// </summary>
        public decimal Weight { get; set; }

        /// <summary>
        /// The composite search score for this page, combining match and weight.
        /// </summary>
        public decimal Score { get; set; }

        /// <summary>
        /// The display title of the page, derived from the name by stripping the namespace prefix if present.
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

        /// <summary>
        /// The namespace this page belongs to, derived from the name prefix before "::". Returns an empty string if no namespace is present.
        /// </summary>
        public string Namespace
        {
            get
            {
                if (Name.Contains("::"))
                {
                    return Name.Substring(0, Name.IndexOf("::")).Trim();
                }
                return string.Empty;
            }
        }
    }
}
