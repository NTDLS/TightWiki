using TightWiki.Plugin.Models;

namespace TightWiki.ViewModels.Admin
{
    public class DeletedPageViewModel
        : TwViewModel
    {
        public int PageId { get; set; }
        public string Body { get; set; } = string.Empty;
        public string DeletedByUserName { get; set; } = string.Empty;
        public DateTime DeletedDate { get; set; }
        public List<TwPageComment> Comments { get; set; } = new();
    }
}
