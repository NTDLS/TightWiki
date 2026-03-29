using TightWiki.Plugin.Models;

namespace TightWiki.Models.ViewModels.Admin
{
    public class PageStatisticsViewModel
        : ViewModelBase
    {
        public List<TwPageStatistics> Statistics { get; set; } = new();
        public int PaginationPageCount { get; set; }
    }
}
