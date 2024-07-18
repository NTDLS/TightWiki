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

        public string Name { get; set; } = string.Empty;
        public string Navigation { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public int Revision { get; set; }
        public int LatestRevision { get; set; }
    }
}
