namespace TightWiki.Repository
{
    /// <summary>
    /// Identifies one revision of a page file, which is also how its content is located in the attachment storage.
    /// </summary>
    public class TwPageFileRevisionKey
    {
        public int PageFileId { get; set; }
        public int Revision { get; set; }

        /// <summary>
        /// The page file's name, when the query provides it. Its extension is the stored file's extension.
        /// </summary>
        public string? Name { get; set; }
    }
}
