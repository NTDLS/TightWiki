namespace TightWiki.Models.ViewModels.Page
{
    public class PageRevertViewModel : ViewModelBase
    {
        public string? PageName { get; set; }
        /// <summary>
        /// The highest revision for the page.
        /// </summary>
        public int HighestRevision { get; set; }
        /// <summary>
        /// The number of revisions that are higher than the current page revision.
        /// </summary>
        public int HigherRevisionCount { get; set; }
    }
}
