namespace TightWiki.Shared.Models.Data
{
    public partial class RelatedPage
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Navigation { get; set; }
        public string Description { get; set; }
        public int Matches { get; set; }
        public int PaginationSize { get; set; }
        public int PaginationCount { get; set; }

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
