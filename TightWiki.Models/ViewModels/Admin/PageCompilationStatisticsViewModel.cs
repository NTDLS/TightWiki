using TightWiki.Models.DataModels;

namespace TightWiki.Models.ViewModels.Admin
{
    public class PageCompilationStatisticsViewModel : ViewModelBase
    {
        public List<PageCompilationStatistics> Statistics { get; set; } = new();
        public int PaginationPageCount { get; set; }
    }
}
