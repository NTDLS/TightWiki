using TightWiki.Library.Interfaces;

namespace TightWiki.Models.DataModels
{
    public class Page : IPage
    {
        public int Id { get; set; } = 0;

        /// <summary>
        /// The revision of this page that is being viewed. May not be the latest revision.
        /// </summary>
        public int Revision { get; set; }
        /// <summary>
        /// The most current revision of this page.
        /// </summary>
        public int MostCurrentRevision { get; set; }

        public bool IsHistoricalVersion => Revision != MostCurrentRevision;

        /// <summary>
        /// Lets us know whether this page exists and is loaded.
        /// </summary>
        public bool Exists => Id > 0;

        /// <summary>
        /// Count of revisions higher than Revision.
        /// </summary>
        public int HigherRevisionCount { get; set; }
        public int DeletedRevisionCount { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ChangeSummary { get; set; } = string.Empty;
        public string Navigation { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int DataHash { get; set; }

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

        public Guid CreatedByUserId { get; set; }
        public DateTime CreatedDate { get; set; }
        public Guid ModifiedByUserId { get; set; }
        public DateTime ModifiedDate { get; set; }

        public DateTime DeletedDate { get; set; }
        public Guid DeletedByUserId { get; set; }
        public string DeletedByUserName { get; set; } = string.Empty;

        public int TokenWeight { get; set; }
        public string Body { get; set; } = string.Empty;
        public string CreatedByUserName { get; set; } = string.Empty;
        public string ModifiedByUserName { get; set; } = string.Empty;


        public int PaginationPageCount { get; set; }
        public decimal Match { get; set; }
        public decimal Weight { get; set; }
        public decimal Score { get; set; }

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
