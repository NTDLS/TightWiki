using TightWiki.Models.DataModels;

namespace TightWiki.Models.ViewModels.Admin
{
    public class PageStatisticsViewModel
        : ViewModelBase
    {
        public List<PageStatistics> Statistics { get; set; } = new();
        public int PaginationPageCount { get; set; }
    }
}
