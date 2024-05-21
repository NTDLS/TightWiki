using System.Collections.Generic;
using TightWiki.DataModels;

namespace TightWiki.ViewModels.Page
{
    public class PageHistoryViewModel : ViewModelBase
    {
        public List<PageRevisionHistory> History { get; set; } = new();
    }
}
