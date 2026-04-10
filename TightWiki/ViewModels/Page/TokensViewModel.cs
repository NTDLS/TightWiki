using TightWiki.Plugin.Models;

namespace TightWiki.ViewModels.Page
{
    public class TokensViewModel
        : TwViewModel
    {
        public List<TwPageToken> Tokens { get; set; } = new();
        public string PageName { get; set; } = string.Empty;
        public string Navigation { get; set; } = string.Empty;
    }
}
