using TightWiki.Plugin.Models;

namespace TightWiki.Models.ViewModels.Page
{
    public class DeletedPageViewModel
        : ViewModelBase
    {
        public int PageId { get; set; }
        public string Body { get; set; } = string.Empty;
        public string DeletedByUserName { get; set; } = string.Empty;
        public DateTime DeletedDate { get; set; }
        public List<TwPageComment> Comments { get; set; } = new();
    }
}
