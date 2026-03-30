using System.Globalization;

namespace TightWiki.ViewModels.Page
{
    public class PageLocalizationViewModel
        : ViewModelBase
    {
        public List<CultureInfo> Languages { get; set; } = new();
    }
}
