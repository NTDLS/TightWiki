namespace TightWiki.Library.Api
{
    /// <summary>
    /// One entry in the revision history of a page.
    /// </summary>
    public class ApiPageRevision
    {
        public int Revision { get; set; }
        public string ModifiedByUserName { get; set; } = string.Empty;
        public DateTime ModifiedDate { get; set; }
        public string ChangeSummary { get; set; } = string.Empty;
    }
}
