namespace TightWiki.Library.Api
{
    /// <summary>
    /// A page as it appears in lists (search results, namespaces, tags).
    /// </summary>
    public class ApiPageSummary
    {
        public string Name { get; set; } = string.Empty;
        public string Navigation { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
