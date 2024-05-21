using TightWiki.Models.DataModels;

namespace TightWiki.Models.ViewModels.Page
{
    public class PageCommentsViewModel : ViewModelBase
    {
        public List<PageComment> Comments { get; set; } = new();
        public string Comment { get; set; } = string.Empty;
    }
}
