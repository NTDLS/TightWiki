namespace TightWiki.Models.ViewModels.Page
{
    public class PageRevertViewModel : ViewModelBase
    {
        public string? PageName { get; set; }
        public int MostCurrentRevision { get; set; }
        public int CountOfRevisions { get; set; }        
    }
}
