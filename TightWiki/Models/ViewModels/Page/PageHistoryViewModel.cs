using TightWiki.Models.DataModels;

namespace TightWiki.Models.ViewModels.Page
{
    public class PageHistoryViewModel : ViewModelBase
    {
        public List<PageRevisionHistory> History { get; set; } = new();
    }
}
