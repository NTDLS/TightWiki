using System.Collections.Generic;
using TightWiki.Library.DataModels;

namespace TightWiki.Library.ViewModels.Admin
{
    public class EmojisViewModel : ViewModelBase
    {
        public List<Emoji> Emojis { get; set; } = new();
        public string Categories { get; set; } = string.Empty;
    }
}
