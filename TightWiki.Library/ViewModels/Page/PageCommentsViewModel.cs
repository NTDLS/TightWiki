using System.Collections.Generic;
using TightWiki.Library.DataModels;

namespace TightWiki.Library.ViewModels.Page
{
    public class PageCommentsViewModel : ViewModelBase
    {
        public List<PageComment> Comments { get; set; } = new();
        public string Comment { get; set; } = string.Empty;
    }
}
