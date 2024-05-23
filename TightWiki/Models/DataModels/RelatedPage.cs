namespace TightWiki.Models.DataModels
{
    public partial class RelatedPage
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Navigation { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Matches { get; set; }
        public int PaginationPageSize { get; set; }
        public int PaginationPageCount { get; set; }

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
