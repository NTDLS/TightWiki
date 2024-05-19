using System;
using System.Collections.Generic;
using TightWiki.Library.DataModels;

namespace TightWiki.Library.ViewModels.Page
{
    public class PageDisplayViewModel : ViewModelBase
    {
        public string Body { get; set; } = string.Empty;
        public string ModifiedByUserName { get; set; } = string.Empty;
        public DateTime ModifiedDate { get; set; }
        public List<PageComment> Comments { get; set; } = new();
    }
}

