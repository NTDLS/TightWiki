using TightWiki.Shared.Models.Data;

namespace TightWiki.ViewModels.Admin
{
    public class MissingPagesViewModel : ViewModelBase
    {
        public List<NonexistentPage> Pages { get; set; } = new();
    }
}
