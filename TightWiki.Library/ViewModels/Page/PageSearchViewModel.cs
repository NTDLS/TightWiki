using System.Collections.Generic;

namespace TightWiki.Library.ViewModels.Page
{
    public class PageSearchViewModel : ViewModelBase
    {
        public List<DataModels.Page> Pages { get; set; } = new();

        public string SearchTokens { get; set; } = string.Empty;
    }
}
