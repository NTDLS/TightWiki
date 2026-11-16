namespace TightWiki.Repository
{
    /// <summary>
    /// Identifies one revision of a page file, which is also how its content is located in the attachment storage.
    /// </summary>
    public class TwPageFileRevisionKey
    {
        public int PageFileId { get; set; }
        public int Revision { get; set; }
    }
}
