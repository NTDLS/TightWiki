namespace TightWiki.Models.DataModels
{
    /// <summary>
    /// Used to cache pre-processed wiki results.
    /// </summary>
    public class PageCache
    {
        /// <summary>
        /// Custom page title set by a call to @@Title("...")
        /// </summary>
        public string? PageTitle { get; set; }
        public string Body { get; set; }

        public PageCache(string body)
        {
            Body = body;

        }
    }
}
