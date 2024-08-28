namespace TightWiki.Models.DataModels
{
    public class NamespaceStat
    {
        public string Namespace { get; set; } = string.Empty;
        public int CountOfPages { get; set; }
        public int PaginationPageCount { get; set; }
    }
}
