namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Used to cache pre-processed wiki results.
    /// </summary>
    public class TwPageCache
    {
        /// <summary>
        /// Custom page title set by a call to @@Title("...")
        /// </summary>
        public string? PageTitle { get; set; }
        public string Body { get; set; }

        public TwPageCache(string body)
        {
            Body = body;

        }
    }
}
