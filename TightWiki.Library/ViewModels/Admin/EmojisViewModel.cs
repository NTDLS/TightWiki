using System.Collections.Generic;
using TightWiki.DataModels;

namespace TightWiki.ViewModels.Admin
{
    public class EmojisViewModel : ViewModelBase
    {
        public List<Emoji> Emojis { get; set; } = new();
        public string Categories { get; set; } = string.Empty;
    }
}
