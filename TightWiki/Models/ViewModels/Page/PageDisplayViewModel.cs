using TightWiki.Models.DataModels;

namespace TightWiki.Models.ViewModels.Page
{
    public class PageDisplayViewModel : ViewModelBase
    {
        public string Body { get; set; } = string.Empty;
        public string ModifiedByUserName { get; set; } = string.Empty;
        public DateTime ModifiedDate { get; set; }
        public List<PageComment> Comments { get; set; } = new();
        public bool HideFooterComments { get; set; }
        public bool HideFooterLastModified { get; set; }
    }
}
