namespace TightWiki.Models.ViewModels.Page
{
    public class DeletedPageRevisionViewModel : ViewModelBase
    {
        public int PageId { get; set; }
        public int Revision { get; set; }
        public string Body { get; set; } = string.Empty;
        public string DeletedByUserName { get; set; } = string.Empty;
        public DateTime DeletedDate { get; set; }
    }
}
