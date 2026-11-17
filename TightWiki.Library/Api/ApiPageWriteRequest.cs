namespace TightWiki.Library.Api
{
    /// <summary>
    /// The body of a request that creates or edits a page. Name is only used when a page is created,
    /// Revision (the revision the edit is based on) is only used when a page is edited.
    /// </summary>
    public class ApiPageWriteRequest
    {
        public string? Name { get; set; }
        public string? Body { get; set; }
        public string? Description { get; set; }
        public string? ChangeSummary { get; set; }
        public int? Revision { get; set; }
    }
}
