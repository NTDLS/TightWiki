using TightWiki.Shared.Models.Data;

namespace TightWiki.Shared.Models.View
{
    public class EmojiModel : ModelBase
    {
        public Emoji Emoji { get; set; }
        public string OriginalName { get; set; }
        public string Categories { get; set; }
    }
}
