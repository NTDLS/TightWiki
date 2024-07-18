namespace TightWiki.Models.DataModels
{
    public class WikiException
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public string ExceptionText { get; set; } = string.Empty;
        public string StackTrace { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public int PaginationPageCount { get; set; }
    }
}
