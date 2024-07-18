namespace TightWiki.Models.ViewModels.Admin
{
    public class OrphanedPageAttachmentsViewModel : ViewModelBase
    {
        public List<DataModels.OrphanedPageAttachment> Files { get; set; } = new();
        public int PaginationPageCount { get; set; }
    }
}
