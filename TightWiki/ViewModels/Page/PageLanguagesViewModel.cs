using System.Globalization;

namespace TightWiki.ViewModels.Page
{
    public class PageLocalizationViewModel
        : TwViewModel
    {
        public List<CultureInfo> Languages { get; set; } = new();
    }
}
