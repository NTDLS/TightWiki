namespace TightWiki.Library.DataModels
{
    public class WikiDatabaseStats
    {
        public int Pages { get; set; }
        public int IntraLinks { get; set; }
        public int PageRevisions { get; set; }
        public int PageAttachments { get; set; }
        public int PageAttachmentRevisions { get; set; }
        public int PageTags { get; set; }
        public int PageSearchTokens { get; set; }
        public int Users { get; set; }
        public int Profiles { get; set; }
        public int Exceptions { get; set; }
    }
}
