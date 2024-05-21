using System.Collections.Generic;
using TightWiki.DataModels;

namespace TightWiki.ViewModels.Page
{
    public class PageCommentsViewModel : ViewModelBase
    {
        public List<PageComment> Comments { get; set; } = new();
        public string Comment { get; set; } = string.Empty;
    }
}
