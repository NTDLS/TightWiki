using TightWiki.Plugin.Models;

namespace TightWiki.Models.ViewModels.Admin
{
    public class EmojisViewModel
        : ViewModelBase
    {
        public List<TwEmoji> Emojis { get; set; } = new();
        public string SearchString { get; set; } = string.Empty;
        public int PaginationPageCount { get; set; }
    }
}
