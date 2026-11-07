namespace TightWiki.Library.Api
{
    /// <summary>
    /// A page with its body in the requested format.
    /// </summary>
    public class ApiPageContent
    {
        public string Name { get; set; } = string.Empty;
        public string Navigation { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Revision { get; set; }
        public int MostCurrentRevision { get; set; }
        public string ModifiedByUserName { get; set; } = string.Empty;
        public DateTime ModifiedDate { get; set; }
        public string Body { get; set; } = string.Empty;
    }
}
