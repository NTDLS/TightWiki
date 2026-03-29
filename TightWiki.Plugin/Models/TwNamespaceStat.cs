namespace TightWiki.Plugin.Models
{
    public class TwNamespaceStat
    {
        public string Namespace { get; set; } = string.Empty;
        public int CountOfPages { get; set; }
        public int PaginationPageCount { get; set; }
    }
}
