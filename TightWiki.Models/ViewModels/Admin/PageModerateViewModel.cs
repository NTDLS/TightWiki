using TightWiki.Plugin.Models;

namespace TightWiki.Models.ViewModels.Admin
{
    public class PageModerateViewModel
        : ViewModelBase
    {
        public List<string> Instructions { get; set; } = new();
        public List<TwPage> Pages { get; set; } = new();
        public string Instruction { get; set; } = string.Empty;
        public int PaginationPageCount { get; set; }
    }
}
