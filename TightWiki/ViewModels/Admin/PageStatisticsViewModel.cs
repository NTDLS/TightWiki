using TightWiki.Plugin.Models;

namespace TightWiki.ViewModels.Admin
{
    public class PageStatisticsViewModel
        : TwViewModel
    {
        public List<TwPageStatistics> Statistics { get; set; } = new();
        public int PaginationPageCount { get; set; }
    }
}
