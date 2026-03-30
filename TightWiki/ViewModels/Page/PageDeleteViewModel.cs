namespace TightWiki.ViewModels.Page
{
    public class PageDeleteViewModel
        : TwViewModel
    {
        public int CountOfAttachments { get; set; }
        public int MostCurrentRevision { get; set; }
        public string? PageName { get; set; }
        public int PageRevision { get; set; }
    }
}
