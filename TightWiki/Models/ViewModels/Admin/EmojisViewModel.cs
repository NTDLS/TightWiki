using TightWiki.Models.DataModels;

namespace TightWiki.Models.ViewModels.Admin
{
    public class EmojisViewModel : ViewModelBase
    {
        public List<Emoji> Emojis { get; set; } = new();
        public string SearchString { get; set; } = string.Empty;
    }
}
