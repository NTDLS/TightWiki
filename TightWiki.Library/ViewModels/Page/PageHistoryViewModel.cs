using System.Collections.Generic;
using TightWiki.Library.DataModels;

namespace TightWiki.Library.ViewModels.Page
{
    public class PageHistoryViewModel : ViewModelBase
    {
        public List<PageRevisionHistory> History { get; set; } = new();
    }
}
