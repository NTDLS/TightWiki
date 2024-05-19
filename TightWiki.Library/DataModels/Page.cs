using System;

namespace TightWiki.Library.DataModels
{
    public class Page
    {
        public int Id { get; set; } = 0;
        public string Name { get; set; } = string.Empty;
        public string Navigation { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public string ElipseDescription
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

        public int Revision { get; set; }
        public Guid CreatedByUserId { get; set; }
        public DateTime CreatedDate { get; set; }
        public Guid ModifiedByUserId { get; set; }
        public DateTime ModifiedDate { get; set; }
        public int TokenWeight { get; set; }
        public string Body { get; set; } = string.Empty;
        public string CreatedByUserName { get; set; } = string.Empty;
        public string ModifiedByUserName { get; set; } = string.Empty;
        public int LatestRevision { get; set; }
        public int PaginationCount { get; set; }
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
