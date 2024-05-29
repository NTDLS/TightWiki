using TightWiki.Models.DataModels;

namespace TightWiki.Models.ViewModels.Admin
{
    public class WikiExceptionViewModel : ViewModelBase
    {
        public WikiException Exception { get; set; } = new();
    }
}
