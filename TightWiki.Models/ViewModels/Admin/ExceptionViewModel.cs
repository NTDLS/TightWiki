using TightWiki.Models.DataModels;

namespace TightWiki.Models.ViewModels.Admin
{
    public class ExceptionViewModel
        : ViewModelBase
    {
        public WikiLogEntry Exception { get; set; } = new();
    }
}
