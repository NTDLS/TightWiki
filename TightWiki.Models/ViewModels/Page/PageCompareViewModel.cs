using DiffPlex.DiffBuilder.Model;

namespace TightWiki.Models.ViewModels.Page
{
    public class PageCompareViewModel : ViewModelBase
    {
        public string ModifiedByUserName { get; set; } = string.Empty;
        public DateTime ModifiedDate { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Navigation { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public int? ThisRevision { get; set; }
        public int? PreviousRevision { get; set; }
        public int? MostCurrentRevision { get; set; }

        public string ChangeSummary { get; set; } = string.Empty;

        public SideBySideDiffModel? DiffModel { get; set; }
    }
}
