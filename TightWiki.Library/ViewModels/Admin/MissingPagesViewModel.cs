using System.Collections.Generic;
using TightWiki.Shared.Models.Data;

namespace TightWiki.Library.ViewModels.Admin
{
    public class MissingPagesViewModel : ViewModelBase
    {
        public List<NonexistentPage> Pages { get; set; } = new();
    }
}
