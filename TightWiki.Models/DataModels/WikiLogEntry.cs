namespace TightWiki.Models.DataModels
{
    public class WikiLogEntry
    {
        public int Id { get; set; }
        public string Severity { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string ExceptionText { get; set; } = string.Empty;
        public string StackTrace { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public int PaginationPageCount { get; set; }
    }
}
