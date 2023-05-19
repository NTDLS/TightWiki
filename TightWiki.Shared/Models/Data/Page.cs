using System;

namespace TightWiki.Shared.Models.Data
{
    public partial class Page
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Navigation { get; set; }
        public string Description { get; set; }

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
        public int CreatedByUserId { get; set; }
        public DateTime CreatedDate { get; set; }
        public int ModifiedByUserId { get; set; }
        public DateTime ModifiedDate { get; set; }
        public int TokenWeight { get; set; }
        public string Body { get; set; }
        public string CreatedByUserName { get; set; }
        public string ModifiedByUserName { get; set; }
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
                return null;
            }
        }
    }
}
