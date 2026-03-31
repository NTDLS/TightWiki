using TightWiki.Plugin.Models;

namespace TightWiki.ViewModels.Admin
{
    public class EmojiViewModel
        : TwViewModel
    {
        public TwEmoji Emoji { get; set; } = new();
        public string OriginalName { get; set; } = string.Empty;
        public string Categories { get; set; } = string.Empty;
    }
}
