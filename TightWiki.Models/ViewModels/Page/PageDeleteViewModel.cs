namespace TightWiki.Models.ViewModels.Page
{
    public class PageDeleteViewModel : ViewModelBase
    {
        public int CountOfAttachments { get; set; }
        public int MostCurrentRevision { get; set; }
        public string? PageName { get; set; }
        public int PageRevision { get; set; }
    }
}
