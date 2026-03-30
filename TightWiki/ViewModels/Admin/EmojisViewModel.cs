using TightWiki.Plugin.Models;

namespace TightWiki.ViewModels.Admin
{
    public class EmojisViewModel
        : TwViewModel
    {
        public List<TwEmoji> Emojis { get; set; } = new();
        public string SearchString { get; set; } = string.Empty;
        public int PaginationPageCount { get; set; }
    }
}
