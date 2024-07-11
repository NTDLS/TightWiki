using TightWiki.Models.DataModels;

namespace TightWiki.Models.ViewModels.Admin
{
    public class PageComplicationStatisticsViewModel : ViewModelBase
    {
        public List<PageComplicationStatistics> Statistics { get; set; } = new();
        public int PaginationPageCount { get; set; }
    }
}
