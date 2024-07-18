using TightWiki.Models.DataModels;

namespace TightWiki.Models.ViewModels.Admin
{
    public class ExceptionViewModel : ViewModelBase
    {
        public WikiException Exception { get; set; } = new();
    }
}
